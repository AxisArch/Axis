using Grasshopper.Kernel.Types;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

/// <summary>
/// This namesspcae proviedes fuctions for
/// the modification of RAPID instructions.
/// </summary>
namespace Axis.Types.RAPID
{
    /// <summary>
    /// RAPID module
    /// </summary>
    public class Module : GH_Goo<Module>
    {
        public List<Program> extraProg = new List<Program>();

        private List<string> declarations = new List<string>
                {
                    "! Declarations",
                    "VAR confdata cData := [0,-1,-1,0];",
                    "VAR extjoint eAxis := [9E9,9E9,9E9,9E9,9E9,9E9];",
                };

        private bool isValid;

        private List<string> legacyProgs = new List<string>();
        private List<Program> main = new List<Program>();
        private string Name;
        private List<Program> progs = new List<Program>();

        private List<string> tag = new List<string>
        {
            "! ABB Robot Code",
            $"! Generated with Axis {Assembly.GetExecutingAssembly().GetName().Version}",
            "! File Created: " + DateTime.Now.ToString(),
            "! Author: " + Environment.UserName.ToString(),
            " ",
        };

        public Module(List<Program> progs = null, List<string> declarations = null, string name = "Submodule")
        {
            if (progs != null)
            {
                foreach (Program prog in progs)
                {
                    if (prog.IsMain)
                    {
                        this.AddMain(prog);
                    }
                    else
                    {
                        if (this.progs == null)
                        {
                            this.progs = new List<Program>();
                        }
                        this.progs.Add(prog);
                    }
                }
            }
            this.Name = name;
            if (declarations != null) { this.declarations = declarations; }
            this.isValid = this.validate();
        }

        public override bool IsValid => isValid;

        public override string TypeDescription => "Represents a RAPID mudule consisting of RAPID procedures";

        public override string TypeName => "RAPID Module";

        public void AddDeclarations(List<string> declaration)
        {
            if (this.declarations == null)
            {
                this.declarations = declaration;
            }
            else
            {
                this.declarations.AddRange(declaration);
            }
        }

        public void AddMain(Program main)
        {
            if (this.main == null)
            {
                this.main = new List<Program>() { main };
            }
            else
            {
                this.main.Add(main);
            }
            this.isValid = this.validate();
        }

        public void AddOverrides(List<string> overrides)
        {
            foreach (Program prog in this.main)
            {
                prog.AddOverrides(overrides);
            }
        }

        public void AddPrograms(List<Program> progs)
        {
            if (progs == null)
            {
                this.progs = progs;
            }
            else
            {
                this.progs.AddRange(progs);
            }
        }

        public void AddPrograms(List<string> progs)
        {
            legacyProgs.AddRange(progs);
        }

        public List<string> Code()
        {
            List<string> mod = new List<string>();
            mod.Add($"MODULE {Name}");
            mod.AddRange(this.tag);
            mod.AddRange(this.declarations);
            mod.Add("");
            mod.Add("! Main Program");
            foreach (Program prog in main)
            {
                mod.AddRange(prog.Code());
            }
            if (legacyProgs.Count > 0) { mod.AddRange(legacyProgs); }
            if (progs.Count > 0)
            {
                mod.Add("! Additional Programs");
                foreach (Program prog in progs)
                {
                    mod.AddRange(prog.Code());
                }
            }
            mod.Add("ENDMODULE");

            return mod;
        }

        public override IGH_Goo Duplicate()
        {
            return this;
        }

        public override string ToString()
        {
            return $"RAPID Module: {Name}";
        }

        private bool ExtraProg(List<Program> extraProg)
        {
            foreach (Program prog in extraProg)
            {
                if (prog.IsMain == true)
                {
                    return false;
                }
            }

            this.extraProg = extraProg;
            return true;
        }

        private bool validate()
        {
            int c = 0;
            foreach (Program prog in this.main)
            {
                if (prog.IsMain == true) { ++c; }
            }
            if (c == 1) { return true; }
            else { return false; }
        }
    }

    /// <summary>
    /// RAPID program
    /// </summary>
    public class Program : GH_Goo<Program>, IEnumerable<string>
    {
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

        private string Name = "ProcName";
        private List<string> overrides = new List<string>();

        public Program(List<string> code = null, List<string> overrides = null, string progName = "ProcName", bool LJ = false, List<string> comments = null)
        {
            if (code != null) this.code = code;
            this.Name = progName;
            this.conL_J = LJ;
            if (overrides != null) { this.overrides = overrides; }
            if (progName == "main") { this.IsMain = true; }
            if (comments != null) { this.comment = comments; }
        }

        public List<string> code { get; private set; }
        public bool IsMain { get; private set; }
        public override bool IsValid => true;

        public override string TypeDescription => "Represents a RAPID procedure that can be combined to a RAPID module";

        public override string TypeName => "RAPID Proc";

        public void Add(string item)
        {
            code.Add(item);
        }

        public void AddOverrides(List<string> overrides)
        {
            this.overrides.AddRange(overrides);
        }

        public List<string> Code()
        {
            var prog = new List<string>();

            prog.AddRange(comment);
            prog.Add($"PROC {Name}()");
            if (this.overrides != null) { prog.AddRange(overrides); }
            prog.AddRange(comment);
            if (conL_J) { prog.AddRange(ljHeader); }
            prog.AddRange(this.code);
            if (conL_J) { prog.AddRange(ljFooter); }

            prog.Add("ENDPROC");

            return prog;
        }

        public override IGH_Goo Duplicate()
        {
            return this;
        }

        public IEnumerator<string> GetEnumerator()
        {
            return code.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public List<Program> ToList()
        {
            return new List<Program> { this };
        }

        public override string ToString()
        {
            return $"RAPID Proc: {Name}";
        }
    }
}