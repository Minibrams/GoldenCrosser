using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GoldenCrosser
{
    class Investment
    {
        public int Id { get; set; }
        public virtual Company Company { get; set; }
        public DateTime BoughtAt { get; }
        public float BoughtPrice { get; set; }
        public float CurrentPrice { get; set; }
        public float Earnings => CurrentPrice - BoughtPrice;
        public string EarningsFormatted {
            get {
                if (Earnings <= 0) return Earnings.ToString("0.00").Replace('.', ',');
                else if (Earnings == 0) return Earnings.ToString("0.00").Replace('.', ',');
                else return '+' + Earnings.ToString("0.00").Replace('.', ',');
            }
        }
        public Brush Indicator {
            get {
                if (Earnings < 0) return Brushes.Red;
                else if (Earnings == 0) return Brushes.Black;
                else return Brushes.DarkGreen;
            } }

        public Investment() {
            BoughtAt = DateTime.Now;
        }
    }
}
