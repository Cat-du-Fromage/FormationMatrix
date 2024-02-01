using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kaizerwald
{
    public static class UnityExtension
    {
        public static void DestroyChildren(this Transform transform)
        {
            for (int i = transform.childCount-1; i > -1; i--)
            {
                Object.Destroy(transform.GetChild(i).gameObject);
            }
        }
    }
}
