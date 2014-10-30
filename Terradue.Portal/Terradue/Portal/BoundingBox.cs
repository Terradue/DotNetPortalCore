using System;
using Terradue.Util;

namespace Terradue.Portal {

    public struct BoundingBox {
    
        private char separator;
        public double MinLon, MinLat, MaxLon, MaxLat;
        public bool Valid; // !!! must be refreshed when coordinate is changed
        
        public BoundingBox(string s) : this(s, ',') {}
        
        public BoundingBox(string s, char separator) {
            Valid = false;
            MinLon = MinLat = MaxLon = MaxLat = 0;
            this.separator = separator;
            if (s == null) return;
            string[] terms = s.Split(separator);
            if (terms.Length != 4) return;
            Valid = true;
            Valid &= Double.TryParse(terms[0], out MinLon) && MinLon >= -180 && MinLon <= 180;
            Valid &= Double.TryParse(terms[1], out MinLat) && MinLat >= -90 && MinLat <= 90;
            Valid &= Double.TryParse(terms[2], out MaxLon) && MaxLon >= MinLon && MaxLon <= 180;
            Valid &= Double.TryParse(terms[3], out MaxLat) && MaxLat >= MinLat && MaxLat <= 180;
        }
        
        public override string ToString() {
            return MinLon.ToString() + separator + MinLat.ToString() + separator + MaxLon.ToString() + separator + MaxLat.ToString();
        }

        public string ToString(char separator) {
            return MinLon.ToString() + separator + MinLat.ToString() + separator + MaxLon.ToString() + separator + MaxLat.ToString();
        }
        
        public string ToPolygonWkt() {
            return String.Format("POLYGON(({0} {1},{2} {1},{2} {3},{0} {3},{0} {1}))", MinLon, MinLat, MaxLon, MaxLat);
        }
        
        public double Area {
            get { return (Valid ? (MaxLon - MinLon) * (MaxLat - MinLat) : 0); }
        }

    }

}    

