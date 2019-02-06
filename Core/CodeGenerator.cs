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
        bool foundLicense = false;
        bool validLicense = false;

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
            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
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

            // Initialize lists to store the robot export code.
            List<string> RAPID = new List<string>();
            List<string> KRL = new List<string>();

            /*
            // Check to see if we have a valid license.
            if (licenseCount == 0)
            {
                var publicKey = File.ReadAllText(Grasshopper.Folders.DefaultAssemblyFolder.ToString() + @"\Axis\publicKey.xml");
                string licensePath = Grasshopper.Folders.DefaultAssemblyFolder.ToString() + @"\Axis\License.xml";

                XmlDocument license = new XmlDocument();
                license.Load(licensePath);

                string licenseStatus = new LicenseValidator(publicKey, licensePath).ValidateXmlDocumentLicense(license);

                if (licenseStatus == "License validated.")
                {
                    licenseCount = 1;
                    this.Message = "License Validated Successfully.";
                }
                else if (licenseStatus == "License has expired.")
                {
                    this.Message = "License Expired";
                }
                else
                {
                    this.Message = "Problem Validating License";
                }
            }
            */
            if (!DA.GetDataList("Program", program)) return;
            if (!DA.GetDataList("Procedures", strProcedures)) return;
            if (!DA.GetData("Path", ref path)) return;
            if (!DA.GetData("File", ref filename)) return;
            if (!DA.GetData("Export", ref export)) return;

            // Get the optional inputs.
            if (modName)
            {
                if (!DA.GetData("Name", ref strModName)) return;
            }
            if (overrides)
            {
                if (!DA.GetDataList("Overrides", strOverrides)) ;
            }
            if (declarations)
            {
                if (!DA.GetDataList("Declarations", strDeclarations)) ;
            }

            // Override license control.
            bool foundLicense = true;

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

            // If we have a valid program and a license, continue..
            if (strProgram != null && foundLicense)
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
                    KRL.Add("; Generated with Axis v0.2");
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
                    RAPID.Add("MODULE " + strModName);
                    RAPID.Add("! ABB Robot Code");
                    RAPID.Add("! Generated with Axis Beta");
                    RAPID.Add("! Created: " + DateTime.Now.ToString());
                    RAPID.Add("! Author: " + Environment.UserName.ToString());
                    RAPID.Add(" ");

                    // Declarations
                    RAPID.Add("! Declarations");
                    RAPID.Add("VAR confdata cData := [0,-1,-1,0];");
                    RAPID.Add("VAR extjoint eAxis := [9E9,9E9,9E9,9E9,9E9,9E9];");

                    for (int i = 0; i < strDeclarations.Count; i++)
                    {
                        RAPID.Add(strDeclarations[i]);
                    }

                    RAPID.Add(" ");

                    // Open Main Proc
                    RAPID.Add("! Main Procedure");
                    RAPID.Add("PROC main()");
                    RAPID.Add(@"ConfL \Off;");
                    RAPID.Add(@"ConfJ \Off;");

                    // Machine overrides
                    if (strOverrides != null) // If we have machine override data, then add it.
                    {
                        for (int i = 0; i < strOverrides.Count; i++)
                        {
                            RAPID.Add(strOverrides[i]);
                        }
                    }
                    RAPID.Add(" ");

                    // Commands
                    RAPID.Add("! Programmed Movement");
                    int progLen = strProgram.Count;
                    if (ignoreLen) this.Message = "No Length Check";
                    if (progLen > 5000 && !ignoreLen) // Check if the main progran is too long and split into subprocedures if this is the case.
                    {
                        RAPID.Add("! Warning: Procedure length exceeds recommend maximum. Program will be split into multiple procedures.");
                        RAPID.Add(" ");
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Procedure length exceeds recommended maximum. Program will be split into multiple procedures.");
                        this.Message = "Large Program";

                        procs = Util.SplitProgram(strProgram, 5000);

                        int procCount = 0;
                        foreach (List<string> proc in procs)
                        {
                            string procName = "SubMain" + procCount.ToString();
                            proc.Insert(0, "PROC " + procName + "()");
                            proc.Insert(proc.Count, "ENDPROC");
                            procCount++;

                            RAPID.Add(procName + ";"); // Add the main call.
                        }
                    }
                    else // Continue as normal and add the rest of the program as text.
                    {
                        for (int i = 0; i < strProgram.Count; i++)
                        {
                            RAPID.Add(strProgram[i]);
                        }
                    }
                    

                    // Close Main Proc
                    RAPID.Add(" ");
                    RAPID.Add("!");
                    RAPID.Add(@"ConfL \On;");
                    RAPID.Add(@"ConfJ \On;");
                    RAPID.Add("ENDPROC");
                    RAPID.Add(" ");

                    // Add the procs from the subdivision of the main module, if relevant.
                    foreach (List<string> proc in procs)
                    {
                        foreach (string line in proc)
                        {
                            RAPID.Add(line);
                        }
                    }

                    // Custom Procs
                    if (strProcedures != null) // If custom RAPID procedures have been specified, add them here.
                    {
                        RAPID.Add("! Custom Procedures");
                        for (int i = 0; i < strProcedures.Count; i++)
                        {
                            RAPID.Add(strProcedures[i]);
                        }
                    }

                    RAPID.Add("ENDMODULE"); // Close out the module.

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
                        this.Message = "Exported";
                    }
                    else
                    {
                        using (StreamWriter module = new StreamWriter(path + @"\" + filename + ".pgf", false))
                        {
                            module.WriteLine(@"<?xml version=""1.0"" encoding=""ISO-8859-1"" ?>");
                            module.WriteLine(@"<Program>");
                            module.WriteLine(@"	<Module>" + strModName + @".mod</Module>");
                            module.WriteLine(@"</Program>");
                        }
                        using (StreamWriter mainProc = new StreamWriter(path + @"\" + strModName + ".mod", false))
                        {
                            for (int i = 0; i < RAPID.Count; i++)
                            {
                                mainProc.WriteLine(RAPID[i]);
                            }
                        }
                        this.Message = "Exported";
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

        /// <summary>
        /// Implement this interface in your component if you want to enable variable parameter UI.
        /// </summary>
        bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, int index) => false;
        bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, int index) => false;
        IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, int index) => null;
        bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, int index) => false;
        void IGH_VariableParameterComponent.VariableParameterMaintenance() { }
    }

    /*
    /// <summary>
    /// License validator validates a license file
    /// that can be located on disk.
    /// </summary>
    public class LicenseValidator : AbstractLicenseValidator
    {
        private readonly string licensePath;
        private string inMemoryLicense;

        /// <summary>
        /// Creates a new instance of <seealso cref="LicenseValidator"/>.
        /// </summary>
        /// <param name="publicKey">public key</param>
        /// <param name="licensePath">path to license file</param>
        public LicenseValidator(string publicKey, string licensePath)
            : base(publicKey)
        {
            this.licensePath = licensePath;
        }

        /// <summary>
        /// Creates a new instance of <seealso cref="LicenseValidator"/>.
        /// </summary>
        /// <param name="publicKey">public key</param>
        /// <param name="licensePath">path to license file</param>
        /// <param name="licenseServerUrl">license server endpoint address</param>
        /// <param name="clientId">Id of the license holder</param>
        public LicenseValidator(string publicKey, string licensePath, string licenseServerUrl, Guid clientId)
            : base(publicKey, licenseServerUrl, clientId)
        {
            this.licensePath = licensePath;
        }

        /// <summary>
        /// Gets or Sets the license content
        /// </summary>
        protected override string License
        {
            get
            {
                return inMemoryLicense ?? File.ReadAllText(licensePath);
            }
            set
            {
                string error = String.Empty ;

                try
                {
                    File.WriteAllText(licensePath, value);
                }
                catch (Exception e)
                {
                    inMemoryLicense = value;                    
                }
            }
        }

        /// <summary>
        /// Validates loaded license
        /// </summary>
        public override void AssertValidLicense()
        {
            if (File.Exists(licensePath) == false)
            {
                throw new LicenseFileNotFoundException();
            }

            base.AssertValidLicense();

        }

        /// <summary>
        /// Removes existing license from the machine.
        /// </summary>
        public override void RemoveExistingLicense()
        {
            File.Delete(licensePath);
        }

        public string ValidateXmlDocumentLicense(XmlDocument doc)
        {
            var id = doc.SelectSingleNode("/license/@CpuId");
            var cpuID = id.Value;

            string currentID = GetHardwareId("Win32_Processor", "processorID");
            if (id == null || cpuID != currentID)
            {
                return "Invalid CPU ID.";
            }

            var serial = doc.SelectSingleNode("/license/@SerialNo");
            if (serial == null)
            {
                return "Invalid serial number.";
            }
            
            var licenseType = doc.SelectSingleNode("/license/@type");
            if (licenseType == null)
            {
                return "Could not find license type.";
            }

            Rhino.Licensing.LicenseType type = (LicenseType)Enum.Parse(typeof(LicenseType), licenseType.Value);

            var name = doc.SelectSingleNode("/license/name/text()");
            var userName = name.Value;
            if (name == null)
            {
                return "Could not find full name in license.";
            }

            DateTime expDT = DateTime.Now;
            var expiration = doc.SelectSingleNode("/license/@expiration");
            var expStr = expiration.Value;
            bool success = DateTime.TryParse(expStr, out expDT);
            if (success)
            {
                if (DateTime.Now.CompareTo(expDT) > 0)
                {
                    return "License has expired.";
                }
            }

            var license = doc.SelectSingleNode("/license");
            foreach (XmlAttribute attrib in license.Attributes)
            {
                if (attrib.Name == "type" || attrib.Name == "expiration" || attrib.Name == "id")
                    continue;

                LicenseAttributes[attrib.Name] = attrib.Value;
            }          

            return "License validated.";
        }

        public static string GetHardwareId(string key, string propertyValue)
        {
            var value = string.Empty;
            var searcher = new ManagementObjectSearcher("select * from " + key);

            foreach (ManagementObject share in searcher.Get())
            {
                value = (string)share.GetPropertyValue(propertyValue);
            }

            return value;
        }
    }
    */
}
 
 
 