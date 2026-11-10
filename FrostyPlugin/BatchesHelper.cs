using System;
using System.Collections.Generic;
using System.Linq;

namespace Frosty.Core
{
    public static class BatchesHelper
    {
        public static List<List<T>> Split<T>(List<T> list, int size)
        {
            if (size <= 0)
            {
                throw new Exception("Batch size cannot be less then 0.");
            }

            var listCopy = new List<T>();
            listCopy.AddRange(list);
            var res = new List<List<T>>();

            while (listCopy.Count > 0)
            {
                var temp = listCopy.Take(size).ToList();
                listCopy = listCopy.Skip(size).ToList();

                res.Add(temp);
            }

            return res;
        }
    }
}
