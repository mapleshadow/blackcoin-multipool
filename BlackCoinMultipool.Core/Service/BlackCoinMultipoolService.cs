﻿using BlackCoinMultipool.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BlackCoinMultipool.Core.Service
{
    public class BlackCoinMultipoolService : IBlackCoinMultipoolService
    {
        private static readonly string _baseUrl = "http://blackcoinpool.com/";

        public async Task<Statistics> GetStatistics(string bitcoinAddress)
        {
            string pageHtml = await GetPage(bitcoinAddress);

            Statistics stats = await ParsePage(pageHtml);
            stats.Address = bitcoinAddress;

            return stats;
        }

        private async Task<string> GetPage(string bitcoinAddress)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_baseUrl);
                var result = await httpClient.GetAsync(@"?miner=" + bitcoinAddress);
                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                    return await result.Content.ReadAsStringAsync();
                else
                    return string.Empty;
            }
        }

        private Task<Statistics> ParsePage(string pageHtml)
        {
            return Task.Run(() =>
            {
                var statistics = new Statistics();

                try
                {
                    HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                    doc.LoadHtml(pageHtml);

                    //HtmlAgilityPack.HtmlNode bodyNode = doc.DocumentNode.

                    var payouts = doc.DocumentNode.Descendants("h2")
                                    .First(e => e.InnerText == "Latest Payouts").NextSibling.InnerText;
                    string[] payoutsSplitted = payouts.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    statistics.LatestPayoutScrypt = Convert.ToDouble(payoutsSplitted[1]);
                    statistics.LatestPayoutSHA256 = Convert.ToDouble(payoutsSplitted[5]);


                    var hashrate = doc.DocumentNode.Descendants("h2")
                                    .First(e => e.InnerText == "Current hashrate (10 min average)").NextSibling.InnerText;
                    string[] hashrateSplitted = hashrate.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                    statistics.HashRateScrypt = Convert.ToDouble(hashrateSplitted[1]);
                    statistics.HashRateSHA256 = Convert.ToDouble(hashrateSplitted[5]);

                    var shares = doc.DocumentNode.Descendants("h2")
                                    .First(e => e.InnerText == "Current Shares").NextSibling.InnerText;
                    string[] sharesSplitted = shares.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                    statistics.CurrentSharesScrypt = Convert.ToInt64(sharesSplitted[1]);
                    statistics.CurrentSharesSHA256 = Convert.ToInt64(sharesSplitted[4]);

                    statistics.Shifts = new List<Shift>();

                    var shifts = doc.DocumentNode.Descendants("tbody").First().Descendants("tr");
                    foreach (var shift in shifts)
                    {
                        var shiftData = shift.Descendants("td").ToList();
                        var shiftObject = new Shift()
                        {
                            Timestamp = DateTimeOffset.UtcNow,
                            Shares = Convert.ToInt64(shiftData[1].InnerText),
                            TotalShares = Convert.ToInt64(shiftData[2].InnerText),
                            AverageHashrate = Convert.ToDouble(shiftData[3].InnerText),
                            Profitability = Convert.ToDouble(shiftData[4].InnerText),
                            BlackCoinSent = Convert.ToDouble(shiftData[5].InnerText),
                            PayedOut = true
                        };
                        statistics.Shifts.Add(shiftObject);
                    }
                }
                catch { }

                return statistics;
            });
        }
    }
}
