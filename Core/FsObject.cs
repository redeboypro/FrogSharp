using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace FrogSharp.Core
{
    public class FsObject : List<float>
    {
        public static readonly FsObject None = new FsObject();

        public float AsSingle => this.First();
        public bool AsBoolean => Convert.ToBoolean(this.First());
        public string AsString => Convert.ToString(this.First(), CultureInfo.CurrentCulture);
        public int AsInteger => Convert.ToInt32(this.First());

        public void Set(float right, int slot) => this[slot] = right;
        public void Mul(float right, int slot) => this[slot] *= right;
        public void Div(float right, int slot) => this[slot] /= right;
        public void Add(float right, int slot) => this[slot] += right;
        public void Sub(float right, int slot) => this[slot] -= right;
    }
}