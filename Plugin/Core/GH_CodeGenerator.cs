using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Windows.Forms;
using System.Xml;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;

using Rhino.Geometry;
using Axis.Targets;
using static Axis.Properties.Settings;
using RAPID;

namespace Axis.Core
{
    /// <summary>
    /// Generate the output code for a robot program.
    /// </summary>
    public class GH_CodeGenerator : GH_Component, IGH_VariableParameterComponent
    {
        // Sticky variables for the options.
        bool modName = false;
        bool declarations = false;
        bool overrides = false;
        Manufacturer m_Manufacturer = Manufacturer.ABB;
        bool ignoreLen = false;
        bool validToken = false;
        Auth auth = null;

        public GH_CodeGenerator() : base("Code Generator", "Code", "Generate manufacturer-specific robot code from a toolpath.", AxisInfo.Plugin, AxisInfo.TabMain)
        {
        }

        #region IO
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Program", "Program", "Robot program as list of strings.", GH_ParamAccess.list);
            pManager.AddTextParameter("Procedures", "Procedures", "Custom procedures / functions / routines to be appended to program.", GH_ParamAccess.list, "! No Custom Procedures");
            pManager.AddTextParameter("Path", "Path", "File path for code generation.", GH_ParamAccess.item, Environment.SpecialFolder.Desktop.ToString());
            pManager.AddTextParameter("File", "File", "File name for code generation.", GH_ParamAccess.item, "RobotProgram");
            pManager.AddBooleanParameter("Export", "Export", "Export the file as to the path specified [ABB exports as both a .mod and a .prg file. KUKA exports as a .src file.", GH_ParamAccess.item, false);
            for (int i = 0; i < 5; i++) pManager[i].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Code", "Code", "Robot code.", GH_ParamAccess.list);
        }
        #endregion

        /// <summary>
        /// Check the authentification status.
        /// </summary>
        protected override void BeforeSolveInstance()
        {
            // Validate the login token.
            auth = new Auth();
            validToken = auth.IsValid;

            if (!validToken)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Please log in to Axis.");
            }
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string strModName = "MainModule";
            string path = Environment.SpecialFolder.Desktop.ToString();
            string filename = "RobotProgram";
            bool export = false;

            List<GH_ObjectWrapper> program = new List<GH_ObjectWrapper>();

            List<string> strHeaders = new List<string>();
            List<string> strDeclarations = new List<string>();
            List<string> strProgram = new List<string>();
            List<string> strProcedures = new List<string>();
            List<string> strOverrides = new List<string>();

            string smFileName = "Submodule";
            string filePath = ""; // /Axis/LongCode/ or /
            string dirHome = "RemovableDisk1:"; //  HOME: or RemovableDisk1:

            // Initialize lists to store the robot export code.
            List<string> rapidCODE = new List<string>();
            List<string> KRL = new List<string>();
            this.Message = m_Manufacturer.ToString();

            if (!DA.GetDataList("Program", program)) return;
            if (!DA.GetDataList("Procedures", strProcedures)) return;
            if (!DA.GetData("Path", ref path)) return;
            if (!DA.GetData("File", ref filename)) return;
            if (!DA.GetData("Export", ref export)) return;

            // Get the optional inputs.
            if (modName)
                if (!DA.GetData("Name", ref strModName)) return;
            if (overrides)
                if (!DA.GetDataList("Overrides", strOverrides)) ;
            if (declarations)
                if (!DA.GetDataList("Declarations", strDeclarations)) ;

            if (!validToken) return;

            // New RAPID module
            Module module = new Module(name: strModName);

            // Convert targets to strings.
            foreach (GH_ObjectWrapper command in program)
            {
                Type cType = command.Value.GetType();

                if (cType.Name == "GH_String")
                {
                    strProgram.Add(command.Value.ToString() as string);
                }
                else
                {
                    Target targ = command.Value as Target;
                    strProgram.Add(targ.StrRob);
                }
            }

            // If we have a valid program and we are logged in...
            if (strProgram != null)
            {
                if (m_Manufacturer == Manufacturer.Kuka)
                {
                    KRL.Add("&ACCESS RVP");
                    KRL.Add("&REL 26");

                    // Open Main Proc
                    KRL.Add("DEF Axis_Program()");
                    KRL.Add(" ");

                    // Headers
                    KRL.Add("; KUKA Robot Code");
                    KRL.Add("; Generated with Axis 1.0");
                    KRL.Add("; Created: " + DateTime.Now.ToString());
                    KRL.Add(" ");

                    KRL.Add("DECL AXIS HOME");
                    KRL.Add("DECL INT i");
                    KRL.Add(" ");

                    KRL.Add("BAS(#INITMOV, 0)");
                    KRL.Add(" ");

                    // If we have declarations, then use them, otherwise assign standard base and tool settings.
                    if (strDeclarations.Count >= 0)
                    {
                        KRL.Add("; Custom Declarations");
                        for (int i = 0; i < strDeclarations.Count; i++)
                        {
                            KRL.Add(strDeclarations[i]);
                        }
                        KRL.Add(" ");
                    }
                    else
                    {
                        KRL.Add(" ");
                        KRL.Add("; No Custom Declarations");
                        KRL.Add("FDAT_ACT = {TOOL_NO 6,BASE_NO 4,IPO_FRAME #BASE}");
                        KRL.Add(" ");
                    }

                    KRL.Add("HOME = {AXIS: A1 -90, A2 -90, A3 90, A4 0, A5 -30.0, A6 0}");
                    KRL.Add(" ");

                    KRL.Add("$APO.CPTP = 1");
                    KRL.Add("$APO.CVEL = 100");
                    KRL.Add("$APO.CDIS = 1");

                    KRL.Add(" ");
                    KRL.Add("$VEL.CP = 0.5");
                    KRL.Add("FOR i = 1 TO 6");
                    KRL.Add(@"   $VEL_AXIS[i] = 100");
                    KRL.Add(@"   $ACC_AXIS[i] = 30");
                    KRL.Add("ENDFOR");
                    KRL.Add(" ");

                    // Commands
                    KRL.Add("; Programmed Movement");
                    if (strProgram.Count > 5000)
                    {
                        KRL.Add("; Warning: Procedure length exceeds recommended maximum. Advise splitting main proc into sub-procs.");
                        KRL.Add(" ");
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Procedure length exceeds recommended maximum. Advise splitting main proc into sub-procs.");
                        //this.Message = "Large Program";
                    }
                    for (int i = 0; i < strProgram.Count; i++)
                    {
                        KRL.Add(strProgram[i]);
                    }

                    KRL.Add("END");

                    DA.SetDataList(0, KRL);
                }

                // If the user has requested KUKA code...
                else if (m_Manufacturer == Manufacturer.ABB)
                {
                    //Settings for the main Program
                    int bottomLim = 5000; //ABB limit about 5000
                    int topLim = 70000 - bottomLim; //ABB limit about 80.000
                    int progLen = strProgram.Count;

                    if (Enumerable.Range(bottomLim, topLim).Contains(progLen) && !ignoreLen)  // Medium length program. Will be cut into submodules...
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Procedure length exceeds recommended maximum. Program will be split into multiple procedures.");
                        //this.Message = "Large Program";

                        var subs = Util.SplitProgram(strProgram, 5000);
                        List<Program> progs = new List<Program>();
                        List<string> strMain = new List<string>();

                        string subName = "SubProg";

                        for (int i = 0; i < subs.Count; ++i)
                        {
                            progs.Add(new Program(subs[i], progName: subName + i.ToString(), comments: new List<string> { "" }));
                        }
                        for (int i = 0; i < subs.Count; ++i)
                        {
                            strMain.Add(subName + i.ToString() + ";");
                        }

                        var comment = new List<string> {
                            "! Warning: Procedure length exceeds recommended maximum. Program will be split into multiple procedures.",
                            " "
                        };

                        Program main = new Program(strMain, progName: "main", LJ: true, comments: comment);

                        module.AddMain(main);
                        module.AddPrograms(progs);
                    }
                    else if (progLen > topLim && !ignoreLen) // Long program. Will be split up into seperate files...
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Procedure length exceeds recommended maximum. Program will be split into multiple procedures.");
                        //this.Message = "Extra Large Program";

                        var progsStr = Util.SplitProgram(strProgram, 5000);
                        string procname = "progNumber";

                        List<Program> external = new List<Program>();
                        for (int i = 0; i < progsStr.Count; ++i)
                        {
                            external.Add(new Program(progsStr[i], progName: procname + i.ToString()));
                        }

                        // Write the instructions for the main prog.
                        List<string> strMain = new List<string>();
                        strMain.AddRange(new List<string> // Load the first pair
                        {
                            $"StartLoad \\Dynamic, sDirHome \\File:= sFile + \"{smFileName}{0}.mod\", load1;",
                            $"StartLoad \\Dynamic, sDirHome \\File:= sFile + \"{smFileName}{1}.mod\", load2;",
                            ""
                        });

                        for (int i = 0; i < external.Count(); i += 2) // Loop through all subprogs
                        {
                            strMain.AddRange(new List<string>
                            {
                                "WaitLoad load1;",
                                $"% \"{procname}{i}\" %;",
                                $"UnLoad sDirHome \\File:= sFile + \"{smFileName}{i}.mod\";",
                                "",
                            });

                            if (i + 2 < external.Count())
                            {
                                strMain.AddRange(new List<string>
                            {
                                $"StartLoad \\Dynamic, sDirHome \\File:= sFile + \"{smFileName}{i+2}.mod\", load1;",
                            });
                            }

                            if (i + 1 < external.Count())
                            {
                                strMain.AddRange(new List<string>
                            {
                                "",
                                $"WaitLoad load2;",
                                $"% \"{procname}{i+1}\" %;",
                                $"UnLoad sDirHome \\File:= sFile + \"{smFileName}{i+1}.mod\";",
                                ""
                            });
                            }

                            if (i + 3 < external.Count())
                            {
                                strMain.AddRange(new List<string>
                            {
                                $"StartLoad \\Dynamic, sDirHome \\File:= sFile + \"{smFileName}{i+3}.mod\", load2;",
                                ""
                            });
                            }
                        }

                        List<string> dec = new List<string>
                        {
                            "",
                            "!Set directory for loading",
                            "VAR loadsession load1;",
                            "VAR loadsession load2;",
                            $"CONST string sDirHome:= \"{dirHome}\"; ",
                            $"CONST string sFile:= \"{filePath}\";",
                        };

                        var comment = new List<string>
                        {
                            "! Warning: Procedure length exceeds recommended maximum. Program will be split into multiple procedures.",
                            "! That will be loaded successively at runtime",
                            " "
                        };

                        var main = new Program(strMain, progName: "main", comments: comment, LJ: true);
                        module.AddMain(main);
                        module.extraProg = external;
                        module.AddDeclarations(dec);
                    }
                    else // In case the prgram length should explicetly be ignored
                    {
                        module.AddMain(new Program(strProgram, LJ: true, progName: "main"));
                    }

                    if (ignoreLen) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Program length is being ignored");

                    module.AddPrograms(strProcedures);

                    if (declarations) module.AddDeclarations(strDeclarations);
                    if (overrides) module.AddOverrides(strOverrides);

                    if (!module.IsValid) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The program is invalid."); }


                    DA.SetDataList(0, module.Code());
                }
                else AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The selected manufacturer has not yet been implemented.");

                if (export)
                {
                    if (m_Manufacturer == Manufacturer.Kuka)
                    {
                        using (StreamWriter writer = new StreamWriter(path + @"\" + filename + ".src", false))
                        {
                            for (int i = 0; i < KRL.Count; i++)
                            {
                                writer.WriteLine(KRL[i]);
                            }
                        }
                        Util.AutoClosingMessageBox.Show("Export Successful!", "Export", 1300);
                    }
                    else if (m_Manufacturer == Manufacturer.ABB)
                    {
                        File.WriteAllLines($@"{path}\\{filename}.pgf", new List<string>
                        {
                            @"<?xml version=""1.0"" encoding=""ISO-8859-1"" ?>",
                            @"<Program>",
                            @"	<Module>" + strModName + @".mod</Module>",
                            @"</Program>",
                        });
                        File.WriteAllLines($@"{path}\\{strModName}.mod", module.Code());
                        for (int i = 0; i < module.extraProg.Count(); ++i)
                        {
                            File.WriteAllLines($@"{path}\{smFileName}{i}.mod", module.extraProg[i].Code());

                        }

                        Util.AutoClosingMessageBox.Show("Export Successful!", "Export", 1300);
                    }
                    else return;
                }
            }
        }

        #region UI
        // Build a list of optional input and output parameters
        IGH_Param[] inputParams = new IGH_Param[3]
        {
        new Param_String() { Name = "Name", NickName = "Name", Description = "A custom name for the code module." },
        new Param_String() { Name = "Overrides", NickName = "Overrides", Description = "Custom override code for insertion into the main program.", Access = GH_ParamAccess.list },
        new Param_String() { Name = "Declarations", NickName = "Declarations", Description = "Add custom declarations to the headers of the code [zone, tool, speed data etc].", Access = GH_ParamAccess.list },
        };

        // The following functions append menu items and then handle the item clicked event.
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            ToolStripMenuItem moduleName = Menu_AppendItem(menu, "Custom Module Name", modName_Click, true, modName);
            moduleName.ToolTipText = "Provide a custom name for the module and overwrite the default.";
            ToolStripMenuItem declarationsCheck = Menu_AppendItem(menu, "Custom Declarations", declarations_Click, true, declarations);
            declarationsCheck.ToolTipText = "Add custom declarations to the headers of the code [zone, tool, speed data etc].";
            ToolStripMenuItem overrideCheck = Menu_AppendItem(menu, "Custom Overrides", overrides_Click, true, overrides);
            overrideCheck.ToolTipText = "Provide custom overrides at the beginning of the main program.";

            ToolStripSeparator seperator = Menu_AppendSeparator(menu);

            ToolStripMenuItem robotManufacturers = Menu_AppendItem(menu, "Manufacturer");
            robotManufacturers.ToolTipText = "Select the robot manufacturer";
            foreach (string name in typeof(Manufacturer).GetEnumNames())
            {
                ToolStripMenuItem item = new ToolStripMenuItem(name, null, manufacturer_Click);

                if (name == this.m_Manufacturer.ToString()) item.Checked = true;
                robotManufacturers.DropDownItems.Add(item);
            }

            ToolStripSeparator seperator2 = Menu_AppendSeparator(menu);

            ToolStripMenuItem ignore = Menu_AppendItem(menu, "Ignore Program Length", ignoreLen_Click, true, ignoreLen);
            ignore.ToolTipText = "Ignore the length of the program and avoid spliting the main program in subprocedures.";
        }

        private void modName_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("ModName");
            modName = !modName;

            // If the option to define the weight of the tool is enabled, add the input.
            if (modName)
            {
                AddInput(0);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Name"), true);
            }
            ExpireSolution(true);
        }

        private void declarations_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("Declarations");
            declarations = !declarations;

            // If the option to define the weight of the tool is enabled, add the input.
            if (declarations)
            {
                AddInput(2);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Declarations"), true);
            }
            ExpireSolution(true);
        }

        private void overrides_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("Overrides");
            overrides = !overrides;

            // If the option to define the weight of the tool is enabled, add the input.
            if (overrides)
            {
                AddInput(1);
            }
            else
            {
                Params.UnregisterInputParameter(Params.Input.FirstOrDefault(x => x.Name == "Overrides"), true);
            }
            ExpireSolution(true);
        }

        private void manufacturer_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("Manufacturer");
            ToolStripMenuItem currentItem = (ToolStripMenuItem)sender;
            Canvas.Menu.UncheckOtherMenuItems(currentItem);
            this.m_Manufacturer = (Manufacturer)currentItem.Owner.Items.IndexOf(currentItem);
            ExpireSolution(true);
        }

        private void ignoreLen_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("Ignore Program Length");
            ignoreLen = !ignoreLen;
        }

        /// <summary>
        /// Register the new output parameters to our component.
        /// </summary>
        /// <param name="index"></param>
        private void AddInput(int index)
        {
            IGH_Param parameter = inputParams[index];

            if (Params.Input.Any(x => x.Name == parameter.Name))
                Params.UnregisterInputParameter(Params.Input.First(x => x.Name == parameter.Name), true);
            else
            {
                int insertIndex = Params.Input.Count;
                for (int i = 0; i < Params.Input.Count; i++)
                {
                    int otherIndex = Array.FindIndex(inputParams, x => x.Name == Params.Input[i].Name);
                    if (otherIndex > index)
                    {
                        insertIndex = i;
                        break;
                    }
                }

                Params.RegisterInputParam(parameter, insertIndex);
            }
            Params.OnParametersChanged();
            ExpireSolution(true);
        }
        #endregion

        #region Serialization
        // Serialize this instance to a Grasshopper writer object.
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetBoolean("ModName", this.modName);
            writer.SetBoolean("Declarations", this.declarations);
            writer.SetBoolean("Overrides", this.overrides);
            writer.SetInt32("Manufacturer", (int)this.m_Manufacturer);
            writer.SetBoolean("IgnoreLen", this.ignoreLen);
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            this.modName = reader.GetBoolean("ModName");
            this.declarations = reader.GetBoolean("Declarations");
            this.overrides = reader.GetBoolean("Overrides");
            this.m_Manufacturer = (Manufacturer)reader.GetInt32("Manufacturer");
            this.ignoreLen = reader.GetBoolean("IgnoreLen");
            return base.Read(reader);
        }
        #endregion

        #region Component Settings
        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;
        void IGH_VariableParameterComponent.VariableParameterMaintenance() { }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.CodeGen;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("{6dccd5dc-520b-482d-bbb2-93607ba5166f}"); }
        }
        public override GH_Exposure Exposure => GH_Exposure.primary;
        #endregion
    }
}


