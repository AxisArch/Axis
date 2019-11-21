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

namespace Axis.Core
{
    public class CodeGenerator : GH_Component, IGH_VariableParameterComponent
    {
        // Sticky variables for the options.
        bool modName = false;
        bool declarations = false;
        bool overrides = false;
        bool manufacturer = false;
        bool ignoreLen = false;

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

        public CodeGenerator() : base("Code Generator", "Code", "Generate manufacturer-specific robot code from a toolpath.", "Axis", "1. Core")
        {
        }

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

            List<List<string>> procs = new List<List<string>>();
            string smFileName = "Submodule";

            // Initialize lists to store the robot export code.
            List<string> RAPID = new List<string>();
            List<string> KRL = new List<string>();

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
                    if (!manufacturer)
                    {
                        strProgram.Add(targ.StrABB);
                    }
                    else
                    {
                        strProgram.Add(targ.StrKUKA);
                    }
                }
            }

            // If we have a valid program and we are logged in...
            if (strProgram != null)
            {
                if (manufacturer)
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
                        KRL.Add("; Warning: Procedure length exceeds recommend maximum. Advise splitting main proc into sub-procs.");
                        KRL.Add(" ");
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Procedure length exceeds recommend maximum. Advise splitting main proc into sub-procs.");
                        this.Message = "Large Program";
                    }
                    for (int i = 0; i < strProgram.Count; i++)
                    {
                        KRL.Add(strProgram[i]);
                    }

                    KRL.Add("END");

                    DA.SetDataList(0, KRL);
                } // If the user has requested KUKA code...
                else
                {
                    // Headers
                    List<string> header = new List<string>() {
                        "MODULE " + strModName,
                        "! ABB Robot Code",
                        "! Generated with Axis 1.0",
                        "! Created: " + DateTime.Now.ToString(),
                        "! Author: " + Environment.UserName.ToString(),
                        " ",
                    };

                    // Declarations
                    List<string> declarations = new List<string>
                    {
                        "! Declarations",
                        "VAR confdata cData := [0,-1,-1,0];",
                        "VAR extjoint eAxis := [9E9,9E9,9E9,9E9,9E9,9E9];",
                    };
                    declarations.AddRange(strDeclarations);

                    //Main Proc header
                    List<string> mainHead = new List<string>
                    {
                        " ",
                        "! Main Procedure",
                        "PROC main()",
                        @"ConfL \Off;",
                        @"ConfJ \Off;"
                    };
                    // Machine overrides
                    if (strOverrides != null) mainHead.AddRange(strOverrides);
                    mainHead.AddRange(new List<string>
                    {
                        " ",
                        "! Programmed Movement"
                    });

                    // The main body will be delare further down


                    // Close Main Proc
                    List<string> mainFooter = new List<string>
                    {
                        " ",
                        "!",
                        @"ConfL \On;",
                        @"ConfJ \On;",
                        "ENDPROC",
                        " "
                    };

                    // Custom Procs
                    List<string> customProc = new List<string>
                    {
                        "! Custom Procedures"
                    };
                    if (strProcedures != null) customProc.AddRange(strProcedures);

                    // Footer
                    List<string> Footer = new List<string> { "ENDMODULE" }; // Close out the module


                    //Settings for the main Program
                    int bottomLim = 5000; //ABB limit about 5000
                    int topLim = 70000; //ABB limit about 80.000

                    int progLen = strProgram.Count;
                    if (ignoreLen) this.Message = "No Length Check";

                    // Commands / Main Module Body
                    List<string> mainBody = new List<string>();
                    if (progLen < bottomLim ) // Short Program
                    {
                        mainBody.AddRange(strProgram);
                    }             
                    if (Enumerable.Range(bottomLim, topLim).Contains(progLen) && !ignoreLen)  // Medium length program. Will be cut into submodules
                    {
                        mainBody.AddRange(new List<string> {
                            "! Warning: Procedure length exceeds recommend maximum. Program will be split into multiple procedures.",
                            " "
                        });
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Procedure length exceeds recommended maximum. Program will be split into multiple procedures.");
                        this.Message = "Large Program";

                        var subs = Util.SplitProgram(strProgram, 5000);

                        int procCount = 0;
                        foreach (List<string> sub in subs)
                        {
                            string procName = "SubMain" + procCount.ToString();

                            sub.Insert(0, $"PROC {procName}()");
                            sub.Insert(sub.Count, "ENDPROC");
                            
                            mainBody.Add(procName + ";"); // Add the main call.
                            procCount++;
                        }

                        // Add the procs from the subdivision to the custom procedures
                        foreach (List<string> sub in subs) {customProc.AddRange(sub);}

                    }
                    if (progLen > topLim && !ignoreLen) // Long program. Will be split up into seperate files
                    {
                        mainBody.AddRange(new List<string>
                        {
                            "! Warning: Procedure length exceeds recommend maximum. Program will be split into multiple procedures.",
                            "! That will be loaded successively at runtime",
                            " "
                        });
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Procedure length exceeds recommended maximum. Program will be split into multiple procedures.");
                        this.Message = "Extra Large Program";

                        List<string> dec = new List<string>
                        {
                            "!Set directory for loading",
                            "VAR loadsession load1;",
                            "VAR loadsession load2;",

                            "CONST string sDirHome:= \"HOME:\";",// might need adjustment
                            "CONST string sFile:= \"/Axis/LongCode/\";", //might need adjustment
                        };
                        declarations.AddRange(dec);

                        procs = Util.SplitProgram(strProgram, 5000);
                        string procname = "procname";
                        int procCount = 0;

                        //Insert header and footer around the Code
                        foreach (List<string> proc in procs)
                        {     
                            string procName = procname + procCount.ToString();
                            proc.InsertRange(0,  new List<string>
                            {
                                "MODULE subModule" ,
                                $"PROC {procName}()"
                            });
                            proc.InsertRange(proc.Count(), new List<string>
                            {
                                "ENDPROC",
                                "ENDModule"
                            });

                            procCount++;
                        }

                        // Write the instructions for the main body
                        mainBody.AddRange(new List<string> // Load the first pair 
                        {
                            $"StartLoad \\Dynamic, sDirHome \\File:= sFile + \"{smFileName}{0}.mod\", load1;",
                            $"StartLoad \\Dynamic, sDirHome \\File:= sFile + \"{smFileName}{1}.mod\", load2;",
                            ""
                        }); 
                        for (int i = 0; i < procCount; i += 2) // Loop through all sub programms
                        {
                            mainBody.AddRange(new List<string>
                            {
                                "WaitLoad load1;",
                                $"% \"{procname}{i}\" %;",
                                $"UnLoad sDirHome \\File:= sFile + \"{smFileName}{i}.mod\";",
                                "",
                            });

                            if (i+2 < procCount) { mainBody.AddRange(new List<string>
                            {
                                $"StartLoad \\Dynamic, sDirHome \\File:= sFile + \"{smFileName}{i+2}.mod\", load1;",
                                //""
                            }); }

                            if (i+1 < procCount){mainBody.AddRange(new List<string>
                            {
                                "",
                                $"WaitLoad load2;",
                                $"% \"{procname}{i+1}\" %;",
                                $"UnLoad sDirHome \\File:= sFile + \"{smFileName}{i+1}.mod\";",
                                ""
                            });}

                            if (i+3 < procCount) { mainBody.AddRange(new List<string>
                            {
                                $"StartLoad \\Dynamic, sDirHome \\File:= sFile + \"{smFileName}{i+3}.mod\", load2;",
                                ""
                            }); }
                        }
                    }
                    if (ignoreLen) // In case the prgram length should explicetly be ignored
                    {
                        mainBody.AddRange(strProgram);
                    }


                    //Assemble all the sections of the programm
                    RAPID.AddRange(header);
                    RAPID.AddRange(declarations);

                    RAPID.AddRange(mainHead);
                    RAPID.AddRange(mainBody);
                    RAPID.AddRange(mainFooter);

                    RAPID.AddRange(customProc);
                    RAPID.AddRange(Footer);
                    

                    DA.SetDataList(0, RAPID);
                }

                if (export)
                {
                    if (manufacturer)
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
                    else
                    {
                        File.WriteAllLines($@"{path}\\{filename}.pgf",new List<string>
                        {
                            @"<?xml version=""1.0"" encoding=""ISO-8859-1"" ?>",
                            @"<Program>",
                            @"	<Module>" + strModName + @".mod</Module>",
                            @"</Program>",
                        });
                        File.WriteAllLines($@"{path}\\{strModName}.mod", RAPID);
                        for (int i = 0; i < procs.Count(); ++i)
                        {
                            File.WriteAllLines($@"{path}\{smFileName}{i}.mod", procs[i]);

                        }

                        Util.AutoClosingMessageBox.Show("Export Successful!", "Export", 1300);
                    }
                }
            }
        }

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

            ToolStripMenuItem type = Menu_AppendItem(menu, "Generate KRL", type_Click, true, manufacturer);
            type.ToolTipText = "Switch from creating the RAPID code (ABB robot code) to KRL (KUKA robot code).";

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

        private void type_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("Manufacturer");
            manufacturer = !manufacturer;
        }

        private void ignoreLen_Click(object sender, EventArgs e)
        {
            RecordUndoEvent("Ignore Program Length");
            ignoreLen = !ignoreLen;
        }

        // Register the new input parameters to our component.
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

        // Serialize this instance to a Grasshopper writer object.
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetBoolean("ModName", this.modName);
            writer.SetBoolean("Declarations", this.declarations);
            writer.SetBoolean("Overrides", this.overrides);
            writer.SetBoolean("Manufacturer", this.manufacturer);
            writer.SetBoolean("IgnoreLen", this.ignoreLen);
            return base.Write(writer);
        }

        // Deserialize this instance from a Grasshopper reader object.
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            this.modName = reader.GetBoolean("ModName");
            this.declarations = reader.GetBoolean("Declarations");
            this.overrides = reader.GetBoolean("Overrides");
            this.manufacturer = reader.GetBoolean("Manufacturer");
            this.ignoreLen = reader.GetBoolean("IgnoreLen");
            return base.Read(reader);
        }

        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;
        void IGH_VariableParameterComponent.VariableParameterMaintenance() { }
    }
}


