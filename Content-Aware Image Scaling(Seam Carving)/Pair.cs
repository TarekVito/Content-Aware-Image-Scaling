using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content_Aware_Image_Scaling_Seam_Carving_
{
    class Pair : IComparable<Pair>
    {
        int first, second;
        public Pair(int f, int s)
        {
            first = f;
            second = s;
        }
        public int CompareTo(Pair other)
        {
            return first.CompareTo(other.first);
        }
    }
}
