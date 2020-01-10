using System;
using System.Collections.Generic;
using System.Text;

namespace MarketQuery
{
    class TradeSearchState
    {
        public int tradePage = 1;
        public static string[] tradeCategories = { "CLOTH", "LIGHTARMOR", "HEAVYARMOR", "PLATEARMOR", "WEAPON", "ACCESSORY", "COMBINE_PART", "GOODS", "MATERIAL", "QUEST", "TIRCOIN", "EVENT", "ETC" };
        public int tradeCategoryNum = 0;
        public int ChunkPageNumber = 1;
        public int uniqueNumber = 1;
        public int seenNumber = 0;
        public void NextSearch(bool IsMoreResult)
        {
            if (IsMoreResult)
            {
                ChunkPageNumber++;
            }
            else
            {
                tradeCategoryNum++;
            }
            uniqueNumber++;
        }
        public bool IsDone()
        {
            return tradeCategoryNum >= tradeCategories.Length;
        }

        public string GetTradeCategory()
        {
            return tradeCategories[tradeCategoryNum];
        }

    }
}
