using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Axis.Tools
{
    public class CharacterList : GH_Component
    {
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Axis.Properties.Resources.CharList;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("2fa5656f-5a70-4715-bdb3-b82f7361c297"); }
        }
        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        public CharacterList() : base("Character List", "Chars", "Create a list of concatenated characters of N length.", "Axis", "8. Workflow")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("N", "N", "The length of the final list.", GH_ParamAccess.item, 26);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("C", "C", "A list of concatenated characters.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int N = 26;

            if (!DA.GetData(0, ref N)) return;

            char[] alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

            List<string> alphabet = new List<string>();
            foreach (char c in alpha)
            {
                alphabet.Add(c.ToString());
            }

            List<string> concats = new List<string>();
            long loops = 0;
            long rem = 0;
            for (int i = 0; i < N; i++)
            {
                if (i < alphabet.Count)
                {
                    concats.Add(alphabet[i]);
                }
                else
                {
                    loops = Math.DivRem(i, alphabet.Count, out rem);
                    int loop = Convert.ToInt32(loops);
                    concats.Add(alphabet[Convert.ToInt32(rem)].ToString() + loop.ToString());
                }
            }

            DA.SetDataList(0, concats);
        }
    }
}