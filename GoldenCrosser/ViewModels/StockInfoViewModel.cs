using GoldenCrosser.Database;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;

namespace GoldenCrosser
{
    class StockInfoViewModel : NotifyPropertyChanged
    {
        #region Commands
        public RelayCommand FindCrossStocks { get; }
        public RelayCommand BuyCrosses { get; }
        public RelayCommand CheckSymbol { get; }
        public RelayCommand LoadInvestments { get; }
        #endregion

        #region Properties
        private string _symbolSearchInput; 
        public string SymbolSearchInput {
            get { return _symbolSearchInput != null ? _symbolSearchInput : ""; }
            set { _symbolSearchInput = value; } }

        private string _stockSymbolName;
        public string StockSymbolName {
            get { return _stockSymbolName; }
            set { _stockSymbolName = value;  OnPropertyChanged(); }  }
        private string _currentPrice;
        public string CurrentPrice {
            get { return _currentPrice; }
            set { _currentPrice = value; OnPropertyChanged(); } }
        private string _companyLoadingMessage;
        public string CompanyLoadingMessage {
            get { return _companyLoadingMessage; }
            set { _companyLoadingMessage = value; OnPropertyChanged(); } }
        private Brush _loadingMessageColor = Brushes.Black;
        public Brush LoadingMessageColor {
            get { return _loadingMessageColor; }
            set { _loadingMessageColor = value; OnPropertyChanged(); } }
        private Company _symbolSearchCompany;
        public Company SymbolSearchCompany {
            get { return _symbolSearchCompany; }
            set { _symbolSearchCompany = value; OnPropertyChanged();
                  OnPropertyChanged("SymbolSearchCompanyVisibility"); } }
        private float _totalEarnings { get { return Investments.Sum(i => i.Earnings); } }
        public string TotalEarnings {
            get {
                if (_totalEarnings <= 0) return _totalEarnings.ToString("0.00").Replace('.', ',');
                else return '+' + _totalEarnings.ToString("0.00").Replace('.', ',');
            }
        }
        public Brush TotalEarningsIndicator {
            get {
                if (_totalEarnings < 0) return Brushes.DarkRed;
                else if (_totalEarnings == 0) return Brushes.Black;
                else return Brushes.DarkGreen;
            }
        }
        public Brush TotalEarningsIndicatorLight {
            get
            {
                if (_totalEarnings < 0) return Brushes.PaleVioletRed;
                else if (_totalEarnings == 0) return Brushes.White;
                else return Brushes.LightGreen;
            }
        }
        public bool SymbolSearchCompanyVisibility { get { return SymbolSearchCompany != null; } }
        #endregion

        #region Fields
        [DllImport("Kernel32")]
        private extern static void AllocConsole();
        private string googleSearchHTML = "http://www.google.com/search?q=";
        private string goldenCrossSearchHTML = "https://trendlyne.com/stock-screeners/simple-moving-average/sma-crossovers/sma-50/above/sma-200/";
        #endregion

        #region Collections
        private AsyncObservableCollection<Company> _companies;
        public AsyncObservableCollection<Company> Companies {
            get { return _companies; }
            set { _companies = value; } }

        private AsyncObservableCollection<Investment> _investments;
        public AsyncObservableCollection<Investment> Investments {
            get { return _investments; }
            set { _investments = value; } }
        #endregion

        #region Methods
        public StockInfoViewModel() {
            Companies = new AsyncObservableCollection<Company>();
            Investments = new AsyncObservableCollection<Investment>();

            FindCrossStocks = new RelayCommand(x => {
                OnFindCrossStocksClicked();
            });

            BuyCrosses = new RelayCommand( x => {
                OnBuyCrossesClicked();
            }, x => Companies.Count != 0);

            CheckSymbol = new RelayCommand(x => {
                OnCheckSymbolClicked();
            });

            LoadInvestments = new RelayCommand(x => {
                OnLoadInvestmentsClicked();
            });

            /* Updating UI elements from different threads requires thread magic: */
            Companies.CollectionChanged += (x,y) => {
                App.Current.Dispatcher.Invoke( () => BuyCrosses.NotifyCanExecuteChanged() );
            };

            Investments.CollectionChanged += (x, y) => {
                OnPropertyChanged("TotalEarnings");
                OnPropertyChanged("TotalEarningsIndicator");
                OnPropertyChanged("TotalEarningsIndicatorLight");
            };
        }

        /// <summary>
        /// Uses Google Finance API to fetch company info of the input stock symbol. 
        /// </summary>
        public Company FetchCompanyInfo(string symbol) {

            string companyObjectPrefix = ",values:[\"";
            string symbolRegex         = @"[A-Z]+";
            string companyNameRegex    = @"[a-zA-Z0-9\.\-()]+";
            string priceRegex          = @"[0-9]+\.[0-9]+";
            Match companyName;
            Match price;

            /* HTML prefixes for the data I'm looking for */
            string companyNamePrefix = /*"<b>" + symbol.ToUpper() +*/ "</b></a><span> - ";
            string currentPricePrefix = "font-size:157%\"><b>";


            var html = new WebClient().DownloadString(googleSearchHTML + symbol + " stock");
            Match companyNameMatch = Regex.Match(html, companyNamePrefix + @"[a-zA-Z0-9\.\-() ]+");
            Match currentPriceMatch = Regex.Match(html, currentPricePrefix + @"[0-9]*,[0-9]+");

            try {
                companyName = Regex.Match(companyNameMatch.Value.Substring(companyObjectPrefix.Length
                                          + symbol.Length + 1), //Skip past the symbol
                                            companyNameRegex);
                price       = Regex.Match(companyNameMatch.Value.Substring(companyObjectPrefix.Length
                                              + symbol.Length
                                              + companyName.Value.Length), //Skip past symbol and company name
                                                priceRegex);
            } catch (ArgumentOutOfRangeException e) {
                /* Either symbol or company name doesn't exist in HTML, disregard this query */
                return null; /* TODO: Handle this exception by construcing Regex Matches in another way. */
            }
            
            if (string.IsNullOrEmpty(companyNameMatch.Value) ||
                string.IsNullOrEmpty(currentPriceMatch.Value)) {
                return null; /* TODO: How the fuck do I catch exceptions from a separate thread */
            }


            /* Format data, save it in a new Company */
            float currentPrice = StringToFloat(
                currentPriceMatch.Value.Substring(currentPricePrefix.Length));
            string companyNameStr = companyNameMatch.Value.Substring(companyNamePrefix.Length-1);
            return new Company() { Name = companyNameStr, CurrentPrice = currentPrice,
                                   Symbol = symbol.ToUpper() }; 
        }

        public void FetchGoldenCrossSymbols() {
            AllocConsole();
            if (Companies.Count != 0) Companies.Clear();

            string symbolPrefix = "<a href=\"/stock/[a-z0-9]+/\">";
            var html = new WebClient().DownloadString(goldenCrossSearchHTML);
            MatchCollection matches = Regex.Matches(html, symbolPrefix + "[A-Z]+");

            try {
                foreach (Match match in matches) {
                    AdvanceLoadingMessage();
                    string symbol = Regex.Match(match.Value, "[A-Z]+").Value;
                    Company c = FetchCompanyInfo(symbol);
                    if (c != null) Companies.Add(c);
                }
                DisplayLoadingMessageSucceeded();
            }
            catch (ArgumentOutOfRangeException e) {
                DisplayLoadingMessageFailed();
            }
        }

        public void OnFindCrossStocksClicked() {
            StartLoadingMessage();
            Task t = new Task( () => FetchGoldenCrossSymbols());
            t.Start(); /* Asynchronously fetch GC companies */
        }

        public void OnBuyCrossesClicked() {
            Task t = new Task( () => CreateCrossInvestments());
            t.Start(); 
        }

        public void CreateCrossInvestments() {
            Investments.Clear();
            foreach (Company c in Companies) {
                Investment i = new Investment() {
                    Company = c,
                    BoughtPrice = c.CurrentPrice,
                    CurrentPrice = c.CurrentPrice
                };
                Investments.Add(i);
            }

            using (var db = new InvestingContext()) {
                Investments.ToList().ForEach(i => {
                    if (!db.Investments.Include("Company").Any(x => x.Company.Symbol == i.Company.Symbol)) {
                        db.Investments.Add(i);
                    }
                });
                db.SaveChanges();
            }
        }

        public void OnCheckSymbolClicked() {
            try {
                Task t = new Task(() => SymbolSearchCompany = FetchCompanyInfo(SymbolSearchInput));
                t.Start(); /* Asynchronously fetch a given company */
            }
            catch (ArgumentOutOfRangeException e) {

            }
        }

        public void OnLoadInvestmentsClicked() {
            Investments.Clear();
            Task t = new Task(() => LoadAllInvestments() );
            t.Start();
        }

        public void LoadAllInvestments() {
            List<Investment> investments = new List<Investment>();

            using (var db = new InvestingContext()) {
                investments = db.Investments.Include("Company").ToList();
            }

            foreach (Investment i in investments) {
                Company c = FetchCompanyInfo(i.Company.Symbol);
                i.CurrentPrice = c.CurrentPrice;
                Investments.Add(i);
            }
        }

        public float StringToFloat(string num) {
            string formatted = num.Replace(',', '.');
            return float.Parse(formatted);
        }

        public string FloatToString(float num) {
            string formatted = num.ToString();
            return formatted.Replace('.', ',');
        }

        public string FloatToString(double num)
        {
            string formatted = num.ToString();
            return formatted.Replace('.', ',');
        }

        public void StartLoadingMessage() {
            LoadingMessageColor = Brushes.Black;
            CompanyLoadingMessage = "Loading."; 
        }

        public void DisplayLoadingMessageSucceeded() {
            CompanyLoadingMessage = "Golden crosses found!";
            LoadingMessageColor = Brushes.LightGreen;
        }

        public void DisplayLoadingMessageFailed() {
            CompanyLoadingMessage = "Issue encountered. Try again.";
            LoadingMessageColor = Brushes.DarkRed;
        }

        public void AdvanceLoadingMessage() {
            if (CompanyLoadingMessage == "Loading...") {
                CompanyLoadingMessage = "Loading.";
            }
            else {
                CompanyLoadingMessage += '.';
            }
        }
        #endregion
    }
}
