using Axis.Kernal;
using GH_IO.Serialization;
using Grasshopper.Kernel.Types;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Linq;
using Grasshopper.Kernel;
using ABB.Robotics.Controllers.RapidDomain;

/// <summary>
/// This namesspcae proviedes fuctions for
/// the modification of RAPID instructions.
/// </summary>
namespace Axis.Types
{
    /// <summary>
    /// RAPID module
    /// </summary>
    public class Module : Program
    {
        new string type = "Rapid";

        // Module File
        private List<Command> declarations = new List<Command>
                {
                    new Command("! Declarations", Manufacturer.ABB),
                    new Command("VAR confdata cData := [0,-1,-1,0];", Manufacturer.ABB),
                    new Command("VAR extjoint eAxis := [9E9,9E9,9E9,9E9,9E9,9E9];", Manufacturer.ABB),
                };
        private Procedure main;
        private List<Procedure> routines = new List<Procedure>();

        // External Modules
        private List<Procedure> externalFiles = new List<Procedure>();


        private bool ignoreLen = false;

        private string filename = string.Empty;
        private string modname = string.Empty;

        #region Methods
        // Public methods
        public override List<string> GetInstructions()
        {
            List<string> mod = new List<string>();
            mod.Add($"MODULE {name}");

            // Set the header
            mod.AddRange(new List<string>
            {
                "! ABB Robot Code",
                $"! Generated with Axis {Assembly.GetExecutingAssembly().GetName().Version}",
                $"! File Created: {DateTime.Now.ToString()}",
                $"! Author: {Environment.UserName.ToString()}",
                " ",
            });

            //Declarations
            mod.AddRange(this.declarations.Select(c => c.RobStr(this.manufacturer)).ToList());
            mod.Add("");

            // Main program
            mod.Add("! Main Program");
            mod.AddRange(main.Code());

            // Aditional routines 
            if (routines.Count > 0)
            {
                mod.Add("! Additional Programs");
                foreach (Procedure prog in routines) mod.AddRange(prog.Code());
            }

            mod.Add("ENDMODULE");

            return mod;
        }
        public override bool SetInstructions(List<Instruction> targets)
        {
            //Settings for the main Program
            int bottomLim = 5000; //ABB limit about 5000
            int topLim = 70000 - bottomLim; //ABB limit about 80.000
            int progLen = targets.Count;
            bool ignoreLen = false;

            string filePath = ""; // /Axis/LongCode/ or /
            string dirHome = "RemovableDisk1:"; //  HOME: or RemovableDisk1:


            /*
             *@ todo Rewrite to be switch statement
             *@ body Rewrite the following if else statemets to be a switch statement. This is dependednt on an upgrade of c# though.
             */
            if (Enumerable.Range(bottomLim, topLim).Contains(progLen) && !ignoreLen)  // Medium length program. Will be cut into submodules...
            {
                /*
                 *@ todo Set Runtime Message
                 *@ body Check if compnent can be accessed from within a class
                 */
                //AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Procedure length exceeds recommended maximum. Program will be split into multiple procedures.");
                //this.Message = "Large Program";

                var subs = Util.SplitProgram(targets, 5000);
                List<Module.Procedure> progs = new List<Module.Procedure>();
                List<Instruction> strMain = new List<Instruction>();

                string subName = "SubProg";

                for (int i = 0; i < subs.Count; ++i) progs.Add(new Procedure(subs[i], progName: subName + i.ToString(), comments: new List<string> { "" }));
                for (int i = 0; i < subs.Count; ++i) strMain.Add(new Command($"{subName}{i};", Manufacturer.ABB));

                var comment = new List<string> {
                            "! Warning: Procedure length exceeds recommended maximum. Program will be split into multiple procedures.",
                            " "
                        };

                main = new Procedure(strMain, progName: "main", LJ: true, comments: comment);

            }
            else if (progLen > topLim && !ignoreLen) // Long program. Will be split up into seperate files...
            {


                /*
                 *@ todo Set Runtime Message
                 *@ body Check if compnent can be accessed from within a class
                 */
                //AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Procedure length exceeds recommended maximum. Program will be split into multiple procedures.");
                //this.Message = "Extra Large Program";

                // Split instructions in to sub files
                string procname = "procsNumber";
                var progsStr = Util.SplitProgram(targets, 5000);

                // Create external modules
                List<Procedure> externals = new List<Procedure>();
                for (int i = 0; i < progsStr.Count; ++i) externals.Add(new Procedure(progsStr[i], progName: $"{procname}{i}"));
   
                // Write the instructions for the main prog.
                var strMain = InstractionsFromProcedures(externals);

                var comment = new List<string>
                        {
                            "! Warning: Procedure length exceeds recommended maximum. Program will be split into multiple procedures.",
                            "! That will be loaded successively at runtime",
                            " "
                        };

                // Set the files
                main = new Module.Procedure(strMain, progName: "main", comments: comment, LJ: true);
                externalFiles = externals;
                declarations.AddRange(new List<Command>
                        {
                            new Command("", Manufacturer.ABB),
                            new Command("!Set directory for loading", Manufacturer.ABB),
                            new Command("VAR loadsession load1;", Manufacturer.ABB),
                            new Command("VAR loadsession load2;", Manufacturer.ABB),
                            new Command($"CONST string sDirHome:= \"{dirHome}\"; ", Manufacturer.ABB),
                            new Command($"CONST string sFile:= \"{filePath}\";", Manufacturer.ABB),
                        }) ;
            }
            else // In case the prgram length should explicetly be ignored
            {
                main = new Procedure(targets, LJ: true, progName: "main");
            }

            if (ignoreLen) 
            {
                /*
                 *@ todo Set Runtime Message
                 *@ body Check if compnent can be accessed from within a class
                 */
                //AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Program length is being ignored"); 
            }

            //if (overrides) AddOverrides(strOverrides);
            return true;
        }
        public override bool Export(string filepath)
        {
            // Write the xml Program file
            File.WriteAllLines($"{filepath}\\{filename}.pgf", new List<string>
                        {
                            $"<?xml version=\"\"1.0\"\" encoding=\"\"ISO-8859-1\"\" ?>",
                            $"<Program>",
                            $"	<Module>{modname}.mod</Module>",
                            $"</Program>",
                        });

            // Write the module File
            File.WriteAllLines($"{filepath}\\{modname}.mod", GetInstructions());

            // Write the additional files
            foreach (Procedure files in externalFiles) File.WriteAllLines($"{filepath}\\{files.Name}.mod", files.Code());

            return true;
        }


        // RAPID specific
        public void AddOverrides(List<Instruction> overrides) => main.AddOverrides(overrides);
        public void AddRotines(List<Procedure> routines)
        {
            if (routines == null) this.routines = routines;
            else this.routines.AddRange(routines);
        }


        //Private methods

        /// <summary>
        /// Create main Module for external progs
        /// </summary>
        /// <param name="proces">list of procedures to be called in order</param>
        /// <returns>List of instructions that call the procedures in order</returns>
        private List<Instruction> InstractionsFromProcedures(List<Procedure> proces)
        {
            List<string> temp = new List<string>();

            temp.AddRange(new List<string> // Load the first pair
                        {
                            $"StartLoad \\Dynamic, sDirHome \\File:= sFile + \"{proces[0].Name}{0}.mod\", load1;",
                            $"StartLoad \\Dynamic, sDirHome \\File:= sFile + \"{proces[1].Name}{1}.mod\", load2;",
                            ""
                        });
            for (int i = 0; i < proces.Count(); i += 2) // Loop through all subprogs
            {
                temp.AddRange(new List<string>
                            {
                                "WaitLoad load1;",
                                $"% \"{proces[i].Name}{i}\" %;",
                                $"UnLoad sDirHome \\File:= sFile + \"{proces[i].Name}{i}.mod\";",
                                "",
                            });

                if (i + 2 < proces.Count())
                {
                    temp.AddRange(new List<string>
                            {
                                $"StartLoad \\Dynamic, sDirHome \\File:= sFile + \"{proces[i].Name}{i+2}.mod\", load1;",
                            });
                }

                if (i + 1 < proces.Count())
                {
                    temp.AddRange(new List<string>
                            {
                                "",
                                $"WaitLoad load2;",
                                $"% \"{proces[i].Name}{i+1}\" %;",
                                $"UnLoad sDirHome \\File:= sFile + \"{proces[i].Name}{i+1}.mod\";",
                                ""
                            });
                }

                if (i + 3 < proces.Count())
                {
                    temp.AddRange(new List<string>
                            {
                                $"StartLoad \\Dynamic, sDirHome \\File:= sFile + \"{proces[i].Name}{i+3}.mod\", load2;",
                                ""
                            });
                }
            }
            return new List<Instruction>();
        }
        #endregion

        #region Constructors
        public Module() 
        {
            manufacturer = Manufacturer.ABB;
            type = "RAPID";
            name = string.Empty; 
        }
        public Module(List<Procedure> progs = null, List<string> declarations = null, string name = "Submodule", string filename = "RobotProgram")
        {
            this.name = name;
            this.modname = name;
            this.filename = filename;

            //manufacturer = Manufacturer.ABB;
            //type = "RAPID";
            //
            //if (progs != null)
            //{
            //    foreach (Procedure prog in progs)
            //    {
            //        if (prog.IsMain)
            //        {
            //            this.AddMain(prog);
            //        }
            //        else
            //        {
            //            if (this.progs == null)
            //            {
            //                this.progs = new List<Procedure>();
            //            }
            //            this.progs.Add(prog);
            //        }
            //    }
            //}
            //this.name = name;
            //if (declarations != null) { this.declarations = declarations; }
        }
        #endregion 

        #region Interfaces of Module
        public override bool IsValid => (main!=null);
        public override string IsValidWhyNot => $"";

        public override IGH_Goo Duplicate() => throw new NotImplementedException();


        public override bool Read(GH_IReader reader)
        {
            return base.Read(reader);
        }
        public override bool Write(GH_IWriter writer)
        {
            return base.Write(writer);
        }
        #endregion


        /// <summary>
        /// RAPID program
        /// </summary>
        public class Procedure : Kernal.Procedure
        {
            new string type = "Rapid";
            private List<string> comment = new List<string>();

            private bool conL_J = false;
            private List<string> ljFooter = new List<string>
                {
                    " ",
                    @"ConfL \On;",
                    @"ConfJ \On;",
                };
            private List<string> ljHeader = new List<string>
                {
                    @"ConfL \Off;",
                    @"ConfJ \Off;",
                    "",
                };

            public string Name { get; private set; } = "ProcName";
            public List<Instruction> code { get; private set; }

            private List<Instruction> overrides = new List<Instruction>();
            public bool IsMain { get; private set; }


            /*
             * @todo Implenet variable
             * @body Add this option to pass a variable to the procedure
             */
            public Procedure(List<Instruction> code = null, List<Instruction> overrides = null, string progName = "ProcName", bool LJ = false, List<string> comments = null)
            {
                if (code != null) this.code = code;
                this.Name = progName;
                this.conL_J = LJ;
                if (overrides != null) { this.overrides = overrides; }
                if (progName == "main") { this.IsMain = true; }
                if (comments != null) { this.comment = comments; }
            }

            public void Add(Instruction item)
            {
                code.Add(item);
            }
            public void AddOverrides(List<Instruction> overrides)
            {
                this.overrides.AddRange(overrides);
            }
            public List<string> Code()
            {
                var prog = new List<string>();

                prog.AddRange(comment);
                prog.Add($"PROC {Name}()");
                if (this.overrides != null) { prog.AddRange(overrides.Select(o => o.RobStr(Manufacturer.ABB)).ToList()); }
                prog.AddRange(comment);
                if (conL_J) { prog.AddRange(ljHeader); }
                prog.AddRange(this.code.Select(c => c.RobStr(Manufacturer.ABB)).ToList());
                if (conL_J) { prog.AddRange(ljFooter); }

                prog.Add("ENDPROC");

                return prog;
            }


            #region Interfaces of Program
            public override bool IsValid => true;
            public override string IsValidWhyNot => $"";


            public override IGH_Goo Duplicate() => throw new NotImplementedException();

            // Serialisation
            public bool Write(GH_IWriter writer)
            {
                throw new NotImplementedException();
            }
            public bool Read(GH_IReader reader)
            {
                throw new NotImplementedException();
            }

            #endregion
        }
    }

    public class KPL : Program
    {
        List<string> KRL = new List<string>();
        List<Instruction> declarations;
        string filename;

        public override List<string> GetInstructions()
        {
            return KRL;
        }
        public override bool SetInstructions(List<Instruction> targets)
        {
            KRL.AddRange(new List<string>()
            {
                "&ACCESS RVP",
                "&REL 26",
                // Open Main Proc
                "DEF Axis_Program()",
                " ",
                // Headers
                "; KUKA Robot Code",
                $"; Generated with Axis {Assembly.GetExecutingAssembly().GetName().Version}",
                $"; File Created: {DateTime.Now.ToString()}",
                $"; Author: {Environment.UserName.ToString()}",

                " ",
                "DECL AXIS HOME",
                "DECL INT i",
                " ",

                "BAS(#INITMOV, 0)",
                " ",

        });

            // If we have declarations, then use them, otherwise assign standard base and tool settings.
            if (declarations != null)
            {
                KRL.Add("; Custom Declarations");
                foreach (Instruction instruction in declarations) KRL.Add(instruction.RobStr(Manufacturer.Kuka));
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
            if (targets.Count > 5000)
            {
                KRL.Add("; Warning: Procedure length exceeds recommended maximum. Advise splitting main proc into sub-procs.");
                KRL.Add(" ");
                //AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Procedure length exceeds recommended maximum. Advise splitting main proc into sub-procs.");
                //this.Message = "Large Program";
            }
            for (int i = 0; i < targets.Count; i++)
            {
                KRL.Add(targets[i].RobStr(Manufacturer.Kuka));
            }

            KRL.Add("END");

            return true;
        }
        public override bool Export(string filepath)
        {
            using (StreamWriter writer = new StreamWriter(filepath + @"\" + filename + ".src", false))
            {
                for (int i = 0; i < KRL.Count; i++)
                {
                    writer.WriteLine(KRL[i]);
                }
            }
            return true;
        }


        public KPL() 
        {
            manufacturer = Manufacturer.Kuka;
            type = "KPL";
            name = string.Empty;
        }

        public override bool IsValid => throw new NotImplementedException();
        public override string IsValidWhyNot => throw new NotImplementedException();

        public override IGH_Goo Duplicate()
        {
            throw new NotImplementedException();
        }
    }
}
