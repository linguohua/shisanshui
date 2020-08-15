using System;
using System.Collections.Generic;
using pokerface;

namespace PokerTest
{
    /// <summary>
    /// 大丰关张的扑克牌相关计算代码
    /// </summary>
    internal class AgariIndex
    {
        // 保存所有大丰关张中合法的牌型
        private static Dictionary<Int64, int> agariTable = new Dictionary<long, int>();

        // slots用于把牌按照rank归类，一幅扑克牌从2到JQK到ACE，一共有13个rank，加上大小王一共14个rank
        private static int[] slots = new int[14];

        /// <summary>
        ///  计算手牌的key
        /// key的计算，由于一共14个rank，然后，每一个rank最多有4张牌，那么需要3bit来表达4（2bit只能表达0到3）
        /// 那么14*3=42，一共需要42bit，所以key用int64来表示。在lua中int最大是48bit，因此也符合lua的要求。
        /// </summary>
        /// <param name="hai">手牌列表，例如一个顺子之类</param>
        /// <returns>返回一个int64，如果是lua则返回一个int（lua的int表数范围是48位）</returns>
        static Int64 calcKey(int[] hai)
        {
            for (var i = 0; i < slots.Length; i++)
            {
                slots[i] = 0;
            }

            for (var i = 0; i < hai.Length; i++)
            {
                var h = hai[i];
                slots[h / 4]++;
            }

            for (var i = 0; i < slots.Length; i++)
            {
                if (slots[i] > 4)
                {
                    throw new System.Exception("card type great than 4,card:"+i);
                }
            }

            Int64 key = 0;
            for (var i = 0; i < slots.Length; i++)
            {
                var s = slots[i];
                var sv = (Int64)s;
                key |= ((sv) << (i * 3));
           }

            return key;
        }
        /// <summary>
        /// 根据牌列表，构造MsgCardHand对象
        /// </summary>
        /// <param name="hai">手牌列表</param>
        /// <returns>如果牌列表是一个有效的组合，则返回一个pokerface.MsgCardHand对象，否则返回null</returns>
        public static pokerface.MsgCardHand  agariConvertMsgCardHand(int[] hai)
        {
            var key = calcKey(hai);
            if (!agariTable.ContainsKey(key))
            {
                return null;
            }

            var agari = agariTable[key];
            var ct = (pokerface.CardHandType)(agari & 0x0f);

            var msgCardhand = new pokerface.MsgCardHand();
            msgCardhand.cardHandType = (int)ct;

            // 排序，让大的牌在前面
            Array.Sort(hai, (x, y) =>
            {
                return y - x;
            });

            var cardsNew = new List<int>();
            switch (ct)
            {
                case pokerface.CardHandType.Flush:
                    cardsNew.AddRange(hai);
                    // 对于ACE需要特殊考虑，如果ACE作为类似于12345这样的顺子
                    var isAceSmallest = ((agari & 0x0100) != 0);
                    if (isAceSmallest) {
                        var swp = cardsNew[0];
                        cardsNew.RemoveAt(0);
                        cardsNew.Add(swp);
                    }
                    break;

                case pokerface.CardHandType.TripletPair:
                case pokerface.CardHandType.Triplet2X2Pair:
                    // 确保3张在前面，对子在后面
                    for (var i = 0; i < hai.Length; i++)
                    {
                        var h = hai[i];
                        if (slots[h/4] == 3)
                        {
                            cardsNew.Add(h);
                        }
                    }
                    for (var i = 0; i < hai.Length; i++)
                    {
                        var h = hai[i];
                        if (slots[h / 4] != 3)
                        {
                            cardsNew.Add(h);
                        }
                    }
                    break;
                default:
                    cardsNew.AddRange(hai);
                    break;
            }

            msgCardhand.cards.AddRange(cardsNew);

            // 如果是3个3，而且不包含红桃3，则把牌组改为炸弹，而不是三张
            if (ct == pokerface.CardHandType.Triplet && msgCardhand.cards[0]/4 == (int)pokerface.CardID.R3H/4)
            {
                var foundR3H = false;
                foreach (var c in msgCardhand.cards)
                {
                    if (c == (int)pokerface.CardID.R3H)
                    {
                        foundR3H = true;
                        break;
                    }
                }

                if (!foundR3H)
                {
                    msgCardhand.cardHandType = (int)pokerface.CardHandType.Bomb;
                }
            }

            return msgCardhand;
        }

        /// <summary>
        /// 判断当前的手牌是否大于上一手牌
        /// </summary>
        /// <param name="prevCardHand">上一手牌</param>
        /// <param name="current">当前的手牌</param>
        /// <returns>如果大于则返回true，其他各种情形都会返回false</returns>
        public static bool agariGreatThan(pokerface.MsgCardHand prevCardHand, pokerface.MsgCardHand current)
        {
            // 如果当前的是炸弹
            if (current.cardHandType == (int)pokerface.CardHandType.Bomb)
            {
                // 上一手不是炸弹
                if (prevCardHand.cardHandType != (int)pokerface.CardHandType.Bomb)
                {
                    return true;
                }

                // 上一手也是炸弹，则比较炸弹牌的大小，大丰关张不存在多于4个牌的炸弹
                return current.cards[0]/4 > prevCardHand.cards[0]/4;
            }

            // 如果上一手牌是炸弹
            if (prevCardHand.cardHandType == (int)pokerface.CardHandType.Bomb)
            {
                return false;
            }

            // 必须类型匹配
            if (prevCardHand.cardHandType != current.cardHandType)
            {
                return false;
            }

            // 张数匹配
            if (prevCardHand.cards.Count != current.cards.Count)
            {
                return false;
            }

            // 单张时，2是最大的
            if (prevCardHand.cardHandType == (int)pokerface.CardHandType.Single)
            {
                if (prevCardHand.cards[0] / 4 == 0)
                {
                    return false;
        
                }

                if (current.cards[0] / 4 == 0)
                {
                    return true;
                }
            }

            // 现在只比较最大牌的大小
            return current.cards[0]/4 > prevCardHand.cards[0]/4;
        }

        /// <summary>
        /// 寻找比上一手牌大的所有有效牌组
        /// 这个主要用于自动打牌以及给出提示之类
        /// </summary>
        /// <param name="prev">上一手牌</param>
        /// <param name="hands">当前手上所有的牌</param>
        /// <returns>返回一个牌组列表，如果没有有效牌组，该列表长度为0</returns>
        public static List<pokerface.MsgCardHand> FindGreatThanCardHand(pokerface.MsgCardHand prev, List<int> hands, int specialCardID)
        {
            var prevCT = (pokerface.CardHandType)prev.cardHandType;
            bool isBomb = false;
            List<pokerface.MsgCardHand> tt = null;
            if (specialCardID >= 0)
            {
                tt = new List<MsgCardHand>();
                var cardHand = new MsgCardHand();
                cardHand.cardHandType = (int)pokerface.CardHandType.Single;
                cardHand.cards.AddRange(extractCardByRank(hands, 0, 1));
                tt.Add(cardHand);
                return tt;
            }

            switch (prevCT)
            {
                case pokerface.CardHandType.Bomb:
                    tt = FindBombGreatThan(prev, hands);
                    isBomb = true;
                    break;
                case pokerface.CardHandType.Flush:
                    tt = FindFlushGreatThan(prev, hands);
                    break;
                case pokerface.CardHandType.Single:
                    tt = FindSingleGreatThan(prev, hands);
                    break;
                case pokerface.CardHandType.Pair:
                    tt = FindPairGreatThan(prev, hands);
                    break;
                case pokerface.CardHandType.Pair2X:
                    tt = FindPair2XGreatThan(prev, hands);
                    break;
                case pokerface.CardHandType.Triplet:
                    tt = FindTripletGreatThan(prev, hands);
                    break;
                case pokerface.CardHandType.Triplet2X:
                    tt = FindTriplet2XGreatThan(prev, hands);
                    break;
                case pokerface.CardHandType.Triplet2X2Pair:
                    tt = FindTriplet2X2PairGreatThan(prev, hands);
                    break;
                case pokerface.CardHandType.TripletPair:
                    tt = FindTripletPairGreatThan(prev, hands);
                    break;
            }

            if (!isBomb)
            {
                var tt2 = FindBomb(hands);
                tt.AddRange(tt2);
            }
            return tt;
        }

        /// <summary>
        /// 寻找手牌上的所有炸弹
        /// </summary>
        /// <param name="hands">手上的所有牌</param>
        /// <returns>返回一个牌组列表，如果没有有效牌组，该列表长度为0</returns>
        private static List<MsgCardHand> FindBomb(List<int> hands)
        {
            List<MsgCardHand> cardHands = new List<MsgCardHand>();
            ResetSlots(hands);

            for (var newBombSuitID = 0; newBombSuitID < (int)pokerface.CardID.AH / 4; newBombSuitID++)
            {
                if (slots[newBombSuitID] > 3)
                {
                    var cardHand = new MsgCardHand();
                    cardHand.cardHandType = (int)pokerface.CardHandType.Bomb;
                    cardHand.cards.AddRange(extractCardByRank(hands, newBombSuitID, 4));
                    cardHands.Add(cardHand);
                }
            }

            // 如果有3个ACE，也是炸弹
            if (slots[(int)pokerface.CardID.AH / 4] > 2)
            {
                var cardHand = new MsgCardHand();
                cardHand.cardHandType = (int)pokerface.CardHandType.Bomb;
                cardHand.cards.AddRange(extractCardByRank(hands, (int)pokerface.CardID.AH / 4, 3));
                cardHands.Add(cardHand);
            }


            // 黑桃梅花方块3组成炸弹
            List<int> three = new List<int>();
            foreach (var h in hands)
            {
                if (h / 4 == (int)pokerface.CardID.R3H / 4 && h != (int)pokerface.CardID.R3H)
                {
                    three.Add(h);
                }
            }

            if (three.Count == 3)
            {
                var cardHand = new MsgCardHand();
                cardHand.cardHandType = (int)pokerface.CardHandType.Bomb;
                cardHand.cards.AddRange(three);
                cardHands.Add(cardHand);
            }

            return cardHands;
        }
        /// <summary>
        /// 寻找所有大于上一手"连3张+两对子"的有效组合
        /// </summary>
        /// <param name="prev">上一手牌</param>
        /// <param name="hands">手上的所有牌</param>
        /// <returns>返回一个牌组列表，如果没有有效牌组，该列表长度为0</returns>
        private static List<MsgCardHand> FindTriplet2X2PairGreatThan(MsgCardHand prev, List<int> hands)
        {
            List<MsgCardHand> cardHands = new List<MsgCardHand>();
            ResetSlots(hands);

            var pairLength = 4;

            if (prev.cards.Count > 10) {
                pairLength = 6;
            }

            var flushLen = prev.cards.Count - pairLength;// 减去N个对子
            var bombCardRankID = prev.cards[0] / 4;
            var seqLength = flushLen / 3;
            for (var newBombSuitID = bombCardRankID + 1; newBombSuitID <= (int)pokerface.CardID.AH / 4;)
            {
                var testBombRankID = newBombSuitID;
                var found = true;
                for (var i = 0; i < seqLength; i++)
                {
                    if (slots[testBombRankID - i] < 3)
                    {
                        newBombSuitID = newBombSuitID + 1;

                        found = false;

                        break;

                    }
                }

                // 找到了
                if (found)
                {

                    var left = newBombSuitID + 1 - seqLength;
                    var right = newBombSuitID;

                    var pairCount = 0;
                    List<int> pairAble = new List<int>();
                    for (var testPair = 0; testPair < left; testPair++)
                    {
                        if (slots[testPair] > 1)
                        {
                            pairCount++;
                            pairAble.Add(testPair);
                        }
                    }

                    for (var testPair = right + 1; testPair < (int)pokerface.CardID.JOB / 4; testPair++)
                    {
                        if (slots[testPair] > 1)
                        {
                            pairCount++;
                            pairAble.Add(testPair);
                        }
                    }

                    if (pairCount >= seqLength)
                    {
                        // 此处不在遍历各种对子组合
                        var cardHand = new MsgCardHand();
                        cardHand.cardHandType = (int)pokerface.CardHandType.Triplet2X2Pair;
                        cardHand.cards.AddRange(extractCardByRanks(hands, left, right, 3));
                        var addPairCount = 0;
                        foreach (var pp in pairAble)
                        {
                            cardHand.cards.AddRange(extractCardByRank(hands, pp, 2));
                            addPairCount++;
                            if (addPairCount == seqLength)
                            {
                                break;
                            }
                        }

                        cardHands.Add(cardHand);
                    }

                    newBombSuitID = newBombSuitID + 1;
                }
            }

            return cardHands;
        }
        /// <summary>
        /// 寻找所有大于上一手"3张+对子"的有效组合
        /// </summary>
        /// <param name="prev">上一手牌</param>
        /// <param name="hands">手上的所有牌</param>
        /// <returns>返回一个牌组列表，如果没有有效牌组，该列表长度为0</returns>
        private static List<MsgCardHand> FindTripletPairGreatThan(MsgCardHand prev, List<int> hands)
        {
            List<MsgCardHand> cardHands = new List<MsgCardHand>();
            ResetSlots(hands);

            var flushLen = prev.cards.Count - 2;// 减去对子
            var bombCardRankID = prev.cards[0] / 4;
            var seqLength = flushLen / 3;
            for (var newBombSuitID = bombCardRankID + 1; newBombSuitID <= (int)pokerface.CardID.AH / 4;)
            {
                var testBombRankID = newBombSuitID;
                var found = true;
                for (var i = 0; i < seqLength; i++)
                {
                    if (slots[testBombRankID - i] < 3)
                    {
                        newBombSuitID = newBombSuitID + 1;

                        found = false;

                        break;

                    }
                }

                // 找到了
                if (found)
                {

                    var left = newBombSuitID + 1 - seqLength;
                    var right = newBombSuitID;

                    var pairCount = 0;
                    List<int> pairAble = new List<int>();
                    for (var testPair = 0; testPair < left; testPair++)
                    {
                        if (slots[testPair] > 1)
                        {
                            pairCount++;
                            pairAble.Add(testPair);
                        }
                    }

                    for (var testPair = right+1; testPair < (int)pokerface.CardID.JOB / 4; testPair++)
                    {
                        if (slots[testPair] > 1)
                        {
                            pairCount++;
                            pairAble.Add(testPair);
                        }
                    }

                    if (pairCount > 0)
                    {
                        // 此处不再遍历各个对子
                        var cardHand = new MsgCardHand();
                        cardHand.cardHandType = (int)pokerface.CardHandType.TripletPair;
                        cardHand.cards.AddRange(extractCardByRank(hands, left, 3));
                        cardHand.cards.AddRange(extractCardByRank(hands, pairAble[0], 2));
                        cardHands.Add(cardHand);
                    }

                    newBombSuitID = newBombSuitID + 1;

                }
            }

            return cardHands;
        }
        /// <summary>
        /// 寻找所有大于上一手"3张"的有效组合
        /// </summary>
        /// <param name="prev">上一手牌</param>
        /// <param name="hands">手上的所有牌</param>
        /// <returns>返回一个牌组列表，如果没有有效牌组，该列表长度为0</returns>
        private static List<MsgCardHand> FindTripletGreatThan(MsgCardHand prev, List<int> hands)
        {
            List<MsgCardHand> cardHands = new List<MsgCardHand>();
            ResetSlots(hands);

            var bombCardRankID = prev.cards[0] / 4;

            // 找一个较大的三张
            for (var newBombSuitID = bombCardRankID + 1; newBombSuitID < (int)pokerface.CardID.JOB / 4; newBombSuitID++)
            {
                if (slots[newBombSuitID] > 2)
                {
                    var cardHand = new MsgCardHand();
                    cardHand.cardHandType = (int)pokerface.CardHandType.Triplet;
                    cardHand.cards.AddRange(extractCardByRank(hands, newBombSuitID, 3));
                    cardHands.Add(cardHand);
                }
            }

            return cardHands;
        }
        /// <summary>
        /// 寻找所有大于上一手"连3张"的有效组合
        /// </summary>
        /// <param name="prev">上一手牌</param>
        /// <param name="hands">手上的所有牌</param>
        /// <returns>返回一个牌组列表，如果没有有效牌组，该列表长度为0</returns>
        private static List<MsgCardHand> FindTriplet2XGreatThan(MsgCardHand prev, List<int> hands)
        {
            List<MsgCardHand> cardHands = new List<MsgCardHand>();

            ResetSlots(hands);

            var flushLen = prev.cards.Count;
            var bombCardRankID = prev.cards[0] / 4; // 最大的顺子牌rank
            var seqLength = flushLen /3;
            for (var newBombSuitID = bombCardRankID + 1; newBombSuitID <= (int)pokerface.CardID.AH / 4;)
            {
                var testBombRankID = newBombSuitID;
                var found = true;
                for (var i = 0; i < seqLength; i++)
                {
                    if (slots[testBombRankID - i] < 3)
                    {
                        newBombSuitID = newBombSuitID + 1;

                        found = false;

                        break;

                    }
                }

                // 找到了
                if (found)
                {
                    var cardHand = new MsgCardHand();
                    cardHand.cardHandType = (int)pokerface.CardHandType.Triplet2X;
                    cardHand.cards.AddRange(extractCardByRanks(hands, testBombRankID - seqLength + 1, testBombRankID, 3));
                    cardHands.Add(cardHand);

                    newBombSuitID = newBombSuitID + 1;
                }

                
            }

            return cardHands;
        }
        /// <summary>
        /// 寻找所有大于上一手"连对"的有效组合
        /// </summary>
        /// <param name="prev">上一手牌</param>
        /// <param name="hands">手上的所有牌</param>
        /// <returns>返回一个牌组列表，如果没有有效牌组，该列表长度为0</returns>
        private static List<MsgCardHand> FindPair2XGreatThan(MsgCardHand prev, List<int> hands)
        {
            List<MsgCardHand> cardHands = new List<MsgCardHand>();

            ResetSlots(hands);

            var flushLen = prev.cards.Count;
            var bombCardRankID = prev.cards[0] / 4; // 最大的顺子牌rank
            var seqLength = flushLen / 2;
            for (var newBombSuitID = bombCardRankID + 1; newBombSuitID <= (int)pokerface.CardID.AH / 4;)
            {
                var testBombRankID = newBombSuitID;
                var found = true;
                for (var i = 0; i < seqLength; i++)
                {
                    if (slots[testBombRankID - i] < 2)
                    {
                        newBombSuitID = newBombSuitID + 1;

                        found = false;

                        break;

                    }
                }

                // 找到了
                if (found)
                {
                    var cardHand = new MsgCardHand();
                    cardHand.cardHandType = (int)pokerface.CardHandType.Pair2X;
                    cardHand.cards.AddRange(extractCardByRanks(hands, testBombRankID - seqLength + 1, testBombRankID, 2));
                    cardHands.Add(cardHand);

                    newBombSuitID = newBombSuitID + 1;
                }

               
            }

            return cardHands;
        }
        /// <summary>
        /// 寻找所有大于上一手"对子"的有效组合
        /// </summary>
        /// <param name="prev">上一手牌</param>
        /// <param name="hands">手上的所有牌</param>
        /// <returns>返回一个牌组列表，如果没有有效牌组，该列表长度为0</returns>
        private static List<MsgCardHand> FindPairGreatThan(MsgCardHand prev, List<int> hands)
        {
            List<MsgCardHand> cardHands = new List<MsgCardHand>();
            ResetSlots(hands);

            var bombCardRankID = prev.cards[0] / 4;

            // 找一个较大的对子
            for (var newBombSuitID = bombCardRankID + 1; newBombSuitID < (int)pokerface.CardID.JOB / 4; newBombSuitID++)
            {
                if (slots[newBombSuitID] > 1)
                {
                    var cardHand = new MsgCardHand();
                    cardHand.cardHandType = (int)pokerface.CardHandType.Pair;
                    cardHand.cards.AddRange(extractCardByRank(hands, newBombSuitID, 2));
                    cardHands.Add(cardHand);
                }
            }

            return cardHands;
        }
        /// <summary>
        /// 寻找所有大于上一手"单张"的有效组合
        /// </summary>
        /// <param name="prev">上一手牌</param>
        /// <param name="hands">手上的所有牌</param>
        /// <returns>返回一个牌组列表，如果没有有效牌组，该列表长度为0</returns>
        private static List<MsgCardHand> FindSingleGreatThan(MsgCardHand prev, List<int> hands)
        {
            List<MsgCardHand> cardHands = new List<MsgCardHand>();
            ResetSlots(hands);

            var bombCardRankID = prev.cards[0] / 4;
            if (bombCardRankID == 0)
            {
                // 2已经是最大的单张了
                return cardHands;
            }

            // 找一个较大的单张
            for (var newBombSuitID = bombCardRankID + 1; newBombSuitID < (int)pokerface.CardID.JOB / 4; newBombSuitID++)
            {
                if (slots[newBombSuitID] > 0)
                {
                    var cardHand = new MsgCardHand();
                    cardHand.cardHandType = (int)pokerface.CardHandType.Single;
                    cardHand.cards.AddRange(extractCardByRank(hands, newBombSuitID, 1));
                    cardHands.Add(cardHand);
                }
            }

            // 自己有2，那就是最大
            if (slots[0] > 0)
            {
                var cardHand = new MsgCardHand();
                cardHand.cardHandType = (int)pokerface.CardHandType.Single;
                cardHand.cards.AddRange(extractCardByRank(hands, 0, 1));
                cardHands.Add(cardHand);
            }

            return cardHands;
        }

        /// <summary>
        /// 寻找所有大于上一手"顺子"的有效组合
        /// </summary>
        /// <param name="prev">上一手牌</param>
        /// <param name="hands">手上的所有牌</param>
        /// <returns>返回一个牌组列表，如果没有有效牌组，该列表长度为0</returns>
        private static List<MsgCardHand> FindFlushGreatThan(MsgCardHand prev, List<int> hands)
        {
            List<MsgCardHand> cardHands = new List<MsgCardHand>();

            ResetSlots(hands);

            var flushLen = prev.cards.Count;
            var bombCardRankID = prev.cards[0] / 4; // 最大的顺子牌rank
            var seqLength = flushLen / 1;
            for (var newBombSuitID = bombCardRankID+1; newBombSuitID <= (int)pokerface.CardID.AH/4 ;)
            {
                var testBombRankID = newBombSuitID;
                var found = true;
                for (var i = 0; i < seqLength; i++)
                {
                    if (slots[testBombRankID - i] < 1)
                    {
                        newBombSuitID = newBombSuitID + 1;

                        found = false;

                        break;
        
                    }
                }

                // 找到了
                if (found)
                {
                    var cardHand = new MsgCardHand();
                    cardHand.cardHandType = (int)pokerface.CardHandType.Flush;
                    cardHand.cards.AddRange(extractCardByRanks(hands, testBombRankID- seqLength + 1, testBombRankID, 1));
                    cardHands.Add(cardHand);

                    newBombSuitID = newBombSuitID + 1;
                }

               
            }

            return cardHands;
        }
        /// <summary>
        /// 寻找所有大于上一手"炸弹"的有效组合
        /// </summary>
        /// <param name="prev">上一手牌</param>
        /// <param name="hands">手上的所有牌</param>
        /// <returns>返回一个牌组列表，如果没有有效牌组，该列表长度为0</returns>
        private static List<MsgCardHand> FindBombGreatThan(MsgCardHand prev, List<int> hands)
        {
            // 注意不需要考虑333这种炸弹，因为他是最小的，而现在是寻找一个大于某个炸弹的炸弹
            List<MsgCardHand> cardHands = new List<MsgCardHand>();

            ResetSlots(hands);

            var bombCardRankID = (prev.cards[0]) / 4;
            for (var newBombSuitID = bombCardRankID + 1; newBombSuitID < (int)pokerface.CardID.AH / 4; newBombSuitID++)
            {
                if (slots[newBombSuitID] > 3)
                {
                    var cardHand = new MsgCardHand();
                    cardHand.cardHandType = (int)pokerface.CardHandType.Bomb;
                    cardHand.cards.AddRange(extractCardByRank(hands, newBombSuitID, 4));
                    cardHands.Add(cardHand);
                }
            }

            // 如果有3个ACE，也是炸弹
            if (slots[(int)pokerface.CardID.AH/4] > 2)
            {
                var cardHand = new MsgCardHand();
                cardHand.cardHandType = (int)pokerface.CardHandType.Bomb;
                cardHand.cards.AddRange(extractCardByRank(hands, (int)pokerface.CardID.AH / 4, 3));

                cardHands.Add(cardHand);
            }

            return cardHands;
        }
        /// <summary>
        /// 根据手牌列表填充slots用于查找各种牌
        /// </summary>
        /// <param name="hands">手牌列表</param>
        private static void ResetSlots(List<int> hands)
        {
            for (var i = 0; i < slots.Length; i++)
            {
                slots[i] = 0;
            }

            foreach (var h in hands)
            {
                slots[h / 4]++;
            }
        }
        /// <summary>
        /// 根据rank，从手牌上提取若干张该rank的牌
        /// </summary>
        /// <param name="hands">手牌列表</param>
        /// <param name="rank">rank值</param>
        /// <param name="count">提取张数</param>
        /// <returns></returns>
        static List<int> extractCardByRank(List<int>hands, int rank, int count)
        {
            var extract = new List<int>();
            foreach(var h in hands)
            {
                if (h/4 == rank)
                {
                    extract.Add(h);

                    if (extract.Count == count)
                    {
                        break;
                    }
                }

            }

            return extract;
        }
        /// <summary>
        /// 根据一个rank范围，提取位于该范围的所有牌，每一种牌提取若干张
        /// </summary>
        /// <param name="hands">手牌列表</param>
        /// <param name="rankStart">起始rank</param>
        /// <param name="rankStop">最大的rank</param>
        /// <param name="countEach">每一个rank提取多少张</param>
        /// <returns></returns>
        static List<int> extractCardByRanks(List<int> hands, int rankStart, int rankStop, int countEach)
        {
            var extract = new List<int>();
            for (var rank = rankStart; rank <= rankStop; rank++)
            {
                var ce = 0;
                foreach (var h in hands)
                {
                    if (h / 4 == rank)
                    {
                        extract.Add(h);
                        ce++;
                        if (ce == countEach)
                        {
                            break;
                        }
                    }
                }
            }

            return extract;
        }

        #region 本区仅仅用于自动化工具，客户端不要采纳
        public static pokerface.MsgCardHand SearchLongestDiscardCardHand(List<int> hands, int specialCardID)
        {
            hands.Sort();
            List<pokerface.MsgCardHand> cardHands = new List<MsgCardHand>();
            cardHands.AddRange(SearchLongestFlush(hands));

            cardHands.AddRange(SearchLongestPairX(hands));

            cardHands.AddRange(SearchLongestTriplet2XOrTriplet2X2Pair(hands));

            cardHands.AddRange(SearchUseableTripletOrTripletPair(hands));

            cardHands.AddRange(SearchBomb(hands));

            cardHands.AddRange(SearchUseableSingle(hands));

            cardHands.Sort((x, y) =>
            {
                return y.cards.Count - x.cards.Count;
            });

            var needR3h = specialCardID >= 0;
            if (needR3h)
            {
                foreach(var ch in cardHands)
                {
                    for (var i = 0; i < ch.cards.Count; i++)
                    {
                        if (ch.cards[i] == (int)pokerface.CardID.R3H)
                        {
                            return ch;
                        }
                    }
                }
            }

            return cardHands[0];
        }

        private static List<MsgCardHand> SearchUseableSingle(List<int> hands2)
        {
            List<MsgCardHand> cardHands = new List<MsgCardHand>();
            ResetSlots(hands2);

            // 找一个较大的单张
            for (var newBombSuitID = 1; newBombSuitID < (int)pokerface.CardID.JOB / 4; newBombSuitID++)
            {
                if (slots[newBombSuitID] > 0)
                {
                    var cardHand = new MsgCardHand();
                    cardHand.cardHandType = (int)pokerface.CardHandType.Single;
                    cardHand.cards.AddRange(extractCardByRank(hands2, newBombSuitID, 1));
                    cardHands.Add(cardHand);
                }
            }

            // 自己有2，那就是最大
            if (slots[0] > 0)
            {
                var cardHand = new MsgCardHand();
                cardHand.cardHandType = (int)pokerface.CardHandType.Single;
                cardHand.cards.AddRange(extractCardByRank(hands2, 0, 1));
                cardHands.Add(cardHand);
            }

            return cardHands;
        }

        private static List<MsgCardHand> SearchBomb(List<int> hands)
        {
            List<MsgCardHand> cardHands = new List<MsgCardHand>();
            ResetSlots(hands);

            for (var newBombSuitID = 0; newBombSuitID < (int)pokerface.CardID.AH / 4; newBombSuitID++)
            {
                if (slots[newBombSuitID] > 3)
                {
                    var cardHand = new MsgCardHand();
                    cardHand.cardHandType = (int)pokerface.CardHandType.Bomb;
                    cardHand.cards.AddRange(extractCardByRank(hands, newBombSuitID, 4));
                    cardHands.Add(cardHand);
                }
            }

            // 如果有3个ACE，也是炸弹
            if (slots[(int)pokerface.CardID.AH / 4] > 2)
            {
                var cardHand = new MsgCardHand();
                cardHand.cardHandType = (int)pokerface.CardHandType.Bomb;
                cardHand.cards.AddRange(extractCardByRank(hands, (int)pokerface.CardID.AH / 4, 3));
                cardHands.Add(cardHand);
            }

            // 黑桃梅花方块3组成炸弹
            foreach (var h in hands)
            {
                List<int> three = new List<int>();
                if (h / 4 == (int)pokerface.CardID.R3H / 4 && h != (int)pokerface.CardID.R3H)
                {
                    three.Add(h);
                }

                if (three.Count == 3)
                {
                    var cardHand = new MsgCardHand();
                    cardHand.cardHandType = (int)pokerface.CardHandType.Bomb;
                    cardHand.cards.AddRange(three);
                    cardHands.Add(cardHand);
                }
            }

            return cardHands;
        }

        private static List<MsgCardHand> SearchUseableTripletOrTripletPair(List<int> hands)
        {
            List<MsgCardHand> cardHands = new List<MsgCardHand>();
            ResetSlots(hands);

            for (var newBombSuitID = 0; newBombSuitID <= (int)pokerface.CardID.AH / 4;)
            {
                var testBombRankID = newBombSuitID;
                var found = true;
                for (var i = 0; i < 1; i++)
                {
                    if (slots[testBombRankID - i] < 3)
                    {
                        newBombSuitID = newBombSuitID + 1;

                        found = false;

                        break;

                    }
                }

                // 找到了
                if (found)
                {
                    var cardHand = new MsgCardHand();
                    cardHand.cards.AddRange(extractCardByRank(hands, newBombSuitID, 3));
                    cardHands.Add(cardHand);

                    var left = newBombSuitID ;
                    var right = newBombSuitID;

                    var pairCount = 0;
                    List<int> pairAble = new List<int>();
                    for (var testPair = 0; testPair < left; testPair++)
                    {
                        if (slots[testPair] > 1)
                        {
                            pairCount++;
                            pairAble.Add(testPair);
                        }
                    }

                    for (var testPair = right + 1; testPair < (int)pokerface.CardID.JOB / 4; testPair++)
                    {
                        if (slots[testPair] > 1)
                        {
                            pairCount++;
                            pairAble.Add(testPair);
                        }
                    }

                    if (pairCount > 0)
                    {
                        // 此处不再遍历各个对子
                        cardHand.cards.AddRange(extractCardByRank(hands, pairAble[0], 2));
                        
                    }

                    newBombSuitID = newBombSuitID + 1;

                }
            }

            return cardHands;
        }

        private static List<MsgCardHand> SearchLongestTriplet2XOrTriplet2X2Pair(List<int> hands)
        {
            List<MsgCardHand> cardHands = new List<MsgCardHand>();

            ResetSlots(hands);

            for (var newBombSuitID = 0; newBombSuitID <= (int)pokerface.CardID.AH / 4;)
            {
                var testBombRankID = newBombSuitID;
                for (var i = 0; i < 13; i++)
                {
                    if (slots[testBombRankID + i] < 3)
                    {

                        // 找到了
                        if (i >= 2)
                        {
                            var cardHand = new MsgCardHand();
                            cardHand.cards.AddRange(extractCardByRanks(hands, testBombRankID, testBombRankID + i - 1, 3));
                            cardHands.Add(cardHand);

                            // 寻找2个对子
                            var left = testBombRankID;
                            var right = testBombRankID + i - 1;

                            var pairCount = 0;
                            List<int> pairAble = new List<int>();
                            for (var testPair = 0; testPair < left; testPair++)
                            {
                                if (slots[testPair] > 1)
                                {
                                    pairCount++;
                                    pairAble.Add(testPair);
                                }
                            }

                            for (var testPair = right + 1; testPair < (int)pokerface.CardID.JOB / 4; testPair++)
                            {
                                if (slots[testPair] > 1)
                                {
                                    pairCount++;
                                    pairAble.Add(testPair);
                                }
                            }

                            if (pairCount >= i)
                            {
                                // 此处不在遍历各种对子组合
                                var addPairCount = 0;
                                foreach (var pp in pairAble)
                                {
                                    cardHand.cards.AddRange(extractCardByRank(hands, pp, 2));
                                    addPairCount++;
                                    if (addPairCount == i)
                                    {
                                        break;
                                    }
                                }
                                
                            }
                        }


                        break;

                    }
                }



                newBombSuitID = newBombSuitID + 1;
            }

            return cardHands;
        }

        private static List<MsgCardHand> SearchLongestPairX(List<int> hands)
        {
            List<MsgCardHand> cardHands = new List<MsgCardHand>();

            ResetSlots(hands);

            for (var newBombSuitID = 0; newBombSuitID <= (int)pokerface.CardID.AH / 4;)
            {
                var testBombRankID = newBombSuitID;
                for (var i = 0; i < 13; i++)
                {
                    if (slots[testBombRankID + i] < 2)
                    {

                        // 找到了
                        if (i >= 1)
                        {
                            var cardHand = new MsgCardHand();
                            cardHand.cards.AddRange(extractCardByRanks(hands, testBombRankID, testBombRankID + i - 1, 2));
                            cardHands.Add(cardHand);
                        }

                        break;

                    }
                }

                newBombSuitID = newBombSuitID + 1;
            }


            return cardHands;
        }

        private static List<MsgCardHand> SearchLongestFlush(List<int> hands)
        {
            List<MsgCardHand> cardHands = new List<MsgCardHand>();

            ResetSlots(hands);
            // 简单起见从3开始搜索，不考虑ACE开始的类似12345这种
            for (var newBombSuitID = 1; newBombSuitID <= (int)pokerface.CardID.AH / 4;)
            {
                var testBombRankID = newBombSuitID;
                for (var i = 0; i < 13; i++)
                {
                    if (slots[testBombRankID + i] < 1)
                    {

                        // 找到了
                        if (i > 4)
                        {
                            var cardHand = new MsgCardHand();
                            cardHand.cards.AddRange(extractCardByRanks(hands, testBombRankID , testBombRankID+i-1, 1));
                            cardHands.Add(cardHand);
                        }

                        break;
                    }
                }

                newBombSuitID = newBombSuitID + 1;
            }

            return cardHands;
        }

        #endregion
        static AgariIndex()
        {
            agariTable[0x220006d8] = 0xf9;
            agariTable[0x201b080] = 0xa9;
            agariTable[0x8db002000] = 0xf9;
            agariTable[0x6d8010100] = 0xf9;
            agariTable[0x6d8480080] = 0xf9;
            agariTable[0x4804006d8] = 0xf9;
            agariTable[0x20020806d8] = 0xf9;
            agariTable[0x28000036c0] = 0xf9;
            agariTable[0x413600] = 0xa9;
            agariTable[0x20020db010] = 0xf9;
            agariTable[0xdb400090] = 0xf9;
            agariTable[0x3604000000] = 0xa9;
            agariTable[0x1249240000] = 0x71;
            agariTable[0x20db800] = 0xf9;
            agariTable[0xa00db000] = 0xf9;
            agariTable[0x206d8400] = 0xf9;
            agariTable[0x40001b080] = 0xa9;
            agariTable[0x20000106c0] = 0xa9;
            agariTable[0x2403600] = 0xa9;
            agariTable[0x24036c0080] = 0xf9;
            agariTable[0x800006d0] = 0xa9;
            agariTable[0x20020d8] = 0xa9;
            agariTable[0x1004006d8] = 0xf9;
            agariTable[0x4db080400] = 0xf9;
            agariTable[0x3610400000] = 0xa9;
            agariTable[0x600000000] = 0x36;
            agariTable[0x201b600020] = 0xf9;
            agariTable[0x20100d8] = 0xa9;
            agariTable[0x1b610410] = 0xf9;
            agariTable[0x201b680010] = 0xf9;
            agariTable[0x6d8020400] = 0xf9;
            agariTable[0x120d8] = 0xa9;
            agariTable[0x4db410000] = 0xf9;
            agariTable[0x20db002010] = 0xf9;
            agariTable[0xdb010020] = 0xf9;
            agariTable[0xdb400800] = 0xf9;
            agariTable[0x48201b600] = 0xf9;
            agariTable[0x9b400] = 0xa9;
            agariTable[0x126da000] = 0xf9;
            agariTable[0x20db012000] = 0xf9;
            agariTable[0x2492490000] = 0x105;
            agariTable[0x20800036d0] = 0xf9;
            agariTable[0x24000136c0] = 0xf9;
            agariTable[0x9b010] = 0xa9;
            agariTable[0x2001b000] = 0xa9;
            agariTable[0x1106d8] = 0xf9;
            agariTable[0x249b600000] = 0xf9;
            agariTable[0x6d8012080] = 0xf9;
            agariTable[0x36c0002480] = 0xf9;
            agariTable[0x6d8420] = 0xf9;
            agariTable[0x6c0000090] = 0xa9;
            agariTable[0x6c0080080] = 0xa9;
            agariTable[0x4106d8010] = 0xf9;
            agariTable[0x20026c0] = 0xa9;
            agariTable[0x8836c0] = 0xf9;
            agariTable[0x8001b080] = 0xa9;
            agariTable[0x20026d8080] = 0xf9;
            agariTable[0x6c0090] = 0xa9;
            agariTable[0x1b002400] = 0xa9;
            agariTable[0x4020106d8] = 0xf9;
            agariTable[0x4806da000] = 0xf9;
            agariTable[0x836c0480] = 0xf9;
            agariTable[0x36e2000] = 0xf9;
            agariTable[0x1b700400] = 0xf9;
            agariTable[0x4000106c0] = 0xa9;
            agariTable[0x800db480] = 0xf9;
            agariTable[0x9b000080] = 0xa9;
            agariTable[0x26c0000400] = 0xa9;
            agariTable[0x800936c0] = 0xf9;
            agariTable[0x4db400400] = 0xf9;
            agariTable[0x1209b600] = 0xf9;
            agariTable[0x1081b600] = 0xf9;
            agariTable[0x1001b080] = 0xa9;
            agariTable[0x1b682080] = 0xf9;
            agariTable[0x2083600] = 0xa9;
            agariTable[0x900db010] = 0xf9;
            agariTable[0x20800d8000] = 0xa9;
            agariTable[0x236d0000] = 0xf9;
            agariTable[0x88001b600] = 0xf9;
            agariTable[0x4900db000] = 0xf9;
            agariTable[0x403600010] = 0xa9;
            agariTable[0x3620] = 0xa9;
            agariTable[0x48001b610] = 0xf9;
            agariTable[0x20800db010] = 0xf9;
            agariTable[0x4936c0000] = 0xf9;
            agariTable[0x49249248] = 0xa1;
            agariTable[0x6d8800010] = 0xf9;
            agariTable[0x4db800] = 0xf9;
            agariTable[0x4800806d8] = 0xf9;
            agariTable[0x2003680] = 0xa9;
            agariTable[0xda002000] = 0xa9;
            agariTable[0x6d80a0000] = 0xf9;
            agariTable[0x2600000000] = 0x57;
            agariTable[0x840036c0] = 0xf9;
            agariTable[0x36c0480080] = 0xf9;
            agariTable[0x36c2100000] = 0xf9;
            agariTable[0x492400] = 0xa5;
            agariTable[0x806c0010] = 0xa9;
            agariTable[0x401b610] = 0xf9;
            agariTable[0x4900006d8] = 0xf9;
            agariTable[0x106da400] = 0xf9;
            agariTable[0x2003602000] = 0xa9;
            agariTable[0x2013600000] = 0xa9;
            agariTable[0x209b600010] = 0xf9;
            agariTable[0x1b6d8000] = 0xf8;
            agariTable[0x40011b600] = 0xf9;
            agariTable[0x106d0000] = 0xa9;
            agariTable[0x4000004d8] = 0xa9;
            agariTable[0x41b612000] = 0xf9;
            agariTable[0x6d8010020] = 0xf9;
            agariTable[0x6d0080000] = 0xa9;
            agariTable[0x12400000] = 0x65;
            agariTable[0x100906d8] = 0xf9;
            agariTable[0x20d8080] = 0xa9;
            agariTable[0x8036c0400] = 0xf9;
            agariTable[0x40] = 0x13;
            agariTable[0x6da000800] = 0xf9;
            agariTable[0x26d8480000] = 0xf9;
            agariTable[0x36c2000020] = 0xf9;
            agariTable[0x201b000010] = 0xa9;
            agariTable[0x6d8080800] = 0xf9;
            agariTable[0x26da002000] = 0xf9;
            agariTable[0x36c0800010] = 0xf9;
            agariTable[0x3600002400] = 0xa9;
            agariTable[0x201b002000] = 0xa9;
            agariTable[0x24800006d8] = 0xf9;
            agariTable[0x826d8400] = 0xf9;
            agariTable[0xd8410000] = 0xa9;
            agariTable[0x600000010] = 0x57;
            agariTable[0x800046d8] = 0xf9;
            agariTable[0x820036d0] = 0xf9;
            agariTable[0x44db000] = 0xf9;
            agariTable[0x6d8010410] = 0xf9;
            agariTable[0x3600410000] = 0xa9;
            agariTable[0x100004d8] = 0xa9;
            agariTable[0x9b620] = 0xf9;
            agariTable[0x40201b680] = 0xf9;
            agariTable[0x1b080010] = 0xa9;
            agariTable[0x6d8002480] = 0xf9;
            agariTable[0x20000004d8] = 0xa9;
            agariTable[0x24d8000] = 0xa9;
            agariTable[0x106c0080] = 0xa9;
            agariTable[0x8241b600] = 0xf9;
            agariTable[0xc0080] = 0x57;
            agariTable[0x804d8] = 0xa9;
            agariTable[0x20820036c0] = 0xf9;
            agariTable[0x20900036c0] = 0xf9;
            agariTable[0x89b600] = 0xf9;
            agariTable[0xdb100080] = 0xf9;
            agariTable[0x36c0800080] = 0xf9;
            agariTable[0x92492400] = 0x105;
            agariTable[0x36d2002000] = 0xf9;
            agariTable[0x3700000000] = 0xa9;
            agariTable[0x36d0400400] = 0xf9;
            agariTable[0x200001b010] = 0xa9;
            agariTable[0x806d8090] = 0xf9;
            agariTable[0x201b610400] = 0xf9;
            agariTable[0x3612000000] = 0xa9;
            agariTable[0x1b6c0] = 0xc8;
            agariTable[0x104806d8] = 0xf9;
            agariTable[0x4026d8010] = 0xf9;
            agariTable[0x4836d0000] = 0xf9;
            agariTable[0x49b600080] = 0xf9;
            agariTable[0x200d8] = 0xa9;
            agariTable[0x3602010] = 0xa9;
            agariTable[0x1b080080] = 0xa9;
            agariTable[0x24db000010] = 0xf9;
            agariTable[0x36c0012080] = 0xf9;
            agariTable[0x36c4080000] = 0xf9;
            agariTable[0x820806d8] = 0xf9;
            agariTable[0x20100836c0] = 0xf9;
            agariTable[0xd8800000] = 0xa9;
            agariTable[0x36c2010010] = 0xf9;
            agariTable[0x904006d8] = 0xf9;
            agariTable[0x4000046d8] = 0xf9;
            agariTable[0x126d8010] = 0xf9;
            agariTable[0x92480000] = 0xa5;
            agariTable[0x20000db020] = 0xf9;
            agariTable[0x20136c2000] = 0xf9;
            agariTable[0xdb490000] = 0xf9;
            agariTable[0x6d8092000] = 0xf9;
            agariTable[0x9041b600] = 0xf9;
            agariTable[0x20136c0010] = 0xf9;
            agariTable[0x4036c2400] = 0xf9;
            agariTable[0x1b002080] = 0xa9;
            agariTable[0xdb402400] = 0xf9;
            agariTable[0x36c0080100] = 0xf9;
            agariTable[0x48041b600] = 0xf9;
            agariTable[0x6d8000000] = 0x98;
            agariTable[0x4036c0800] = 0xf9;
            agariTable[0x1249248000] = 0x81;
            agariTable[0x4000006d0] = 0xa9;
            agariTable[0x93600] = 0xa9;
            agariTable[0x4104db000] = 0xf9;
            agariTable[0x9b600800] = 0xf9;
            agariTable[0x4db002400] = 0xf9;
            agariTable[0x3680400000] = 0xa9;
            agariTable[0x600002000] = 0x57;
            agariTable[0x8000106d8] = 0xf9;
            agariTable[0x8000136c0] = 0xf9;
            agariTable[0x3610400] = 0xa9;
            agariTable[0x1b080400] = 0xa9;
            agariTable[0x26da080000] = 0xf9;
            agariTable[0x100000] = 0x42;
            agariTable[0x2480000000] = 0x65;
            agariTable[0xdb002090] = 0xf9;
            agariTable[0xdb410010] = 0xf9;
            agariTable[0xdb480080] = 0xf9;
            agariTable[0x6d8004400] = 0xf9;
            agariTable[0x6da010400] = 0xf9;
            agariTable[0x36c0010090] = 0xf9;
            agariTable[0x3600000020] = 0xa9;
            agariTable[0x10201b600] = 0xf9;
            agariTable[0x9b002000] = 0xa9;
            agariTable[0xd8402000] = 0xa9;
            agariTable[0x3600400010] = 0xa9;
            agariTable[0x36c0490] = 0xf9;
            agariTable[0x100db020] = 0xf9;
            agariTable[0x4006da010] = 0xf9;
            agariTable[0x36e0400] = 0xf9;
            agariTable[0xdb092000] = 0xf9;
            agariTable[0x26d8400010] = 0xf9;
            agariTable[0x20120036c0] = 0xf9;
            agariTable[0x200009b610] = 0xf9;
            agariTable[0x40001b400] = 0xa9;
            agariTable[0x206c0000] = 0xa9;
            agariTable[0x4836c2000] = 0xf9;
            agariTable[0x6dc000400] = 0xf9;
            agariTable[0x40000d8] = 0xa9;
            agariTable[0xdb880] = 0xf9;
            agariTable[0x36d2010] = 0xf9;
            agariTable[0x2003610000] = 0xa9;
            agariTable[0x201b010000] = 0xa9;
            agariTable[0xdb080100] = 0xf9;
            agariTable[0x3602000400] = 0xa9;
            agariTable[0x6d8000] = 0x98;
            agariTable[0x24000106d8] = 0xf9;
            agariTable[0x4104036c0] = 0xf9;
            agariTable[0x4036d0010] = 0xf9;
            agariTable[0x81b600080] = 0xf9;
            agariTable[0x3610000080] = 0xa9;
            agariTable[0x20000c0] = 0x57;
            agariTable[0x100d8400] = 0xa9;
            agariTable[0x106c0400] = 0xa9;
            agariTable[0x2003000] = 0x57;
            agariTable[0x20d8010] = 0xa9;
            agariTable[0x6d8810] = 0xf9;
            agariTable[0x201b682000] = 0xf9;
            agariTable[0x6d8402080] = 0xf9;
            agariTable[0x18080] = 0x57;
            agariTable[0x926d8] = 0xf9;
            agariTable[0x120806d8] = 0xf9;
            agariTable[0x209b610] = 0xf9;
            agariTable[0xdb402080] = 0xf9;
            agariTable[0x200] = 0x13;
            agariTable[0x9001b610] = 0xf9;
            agariTable[0x9009b600] = 0xf9;
            agariTable[0x20006d0000] = 0xa9;
            agariTable[0x4200036c0] = 0xf9;
            agariTable[0x4820db000] = 0xf9;
            agariTable[0x20006da010] = 0xf9;
            agariTable[0x1b700010] = 0xf9;
            agariTable[0x1b602480] = 0xf9;
            agariTable[0x241b000000] = 0xa9;
            agariTable[0x36c0000420] = 0xf9;
            agariTable[0x3000080] = 0x57;
            agariTable[0x20006dc000] = 0xf9;
            agariTable[0x41b400000] = 0xa9;
            agariTable[0x6da000100] = 0xf9;
            agariTable[0x6c2000010] = 0xa9;
            agariTable[0x6c0000480] = 0xa9;
            agariTable[0x6c0400080] = 0xa9;
            agariTable[0x24000db400] = 0xf9;
            agariTable[0x241b000] = 0xa9;
            agariTable[0x1020db000] = 0xf9;
            agariTable[0x906d8400] = 0xf9;
            agariTable[0xdb080410] = 0xf9;
            agariTable[0x36c0004400] = 0xf9;
            agariTable[0x1249249000] = 0x91;
            agariTable[0x46c0] = 0xa9;
            agariTable[0x1000836c0] = 0xf9;
            agariTable[0x4820036c0] = 0xf9;
            agariTable[0x800d8080] = 0xa9;
            agariTable[0x1106d8000] = 0xf9;
            agariTable[0x41b600410] = 0xf9;
            agariTable[0x24d8000000] = 0xa9;
            agariTable[0x36c0490000] = 0xf9;
            agariTable[0x800206d8] = 0xf9;
            agariTable[0x41b600020] = 0xf9;
            agariTable[0x41b602400] = 0xf9;
            agariTable[0x1b400080] = 0xa9;
            agariTable[0x20db000800] = 0xf9;
            agariTable[0xdb012400] = 0xf9;
            agariTable[0x26da010000] = 0xf9;
            agariTable[0x1006da000] = 0xf9;
            agariTable[0x89b600000] = 0xf9;
            agariTable[0x8db000400] = 0xf9;
            agariTable[0x6d8010090] = 0xf9;
            agariTable[0x36c4002000] = 0xf9;
            agariTable[0x36d0090000] = 0xf9;
            agariTable[0x206da000] = 0xf9;
            agariTable[0x8000806d8] = 0xf9;
            agariTable[0x20020d8000] = 0xa9;
            agariTable[0x36c0480010] = 0xf9;
            agariTable[0x3600000800] = 0xa9;
            agariTable[0x2000003000] = 0x57;
            agariTable[0x4000206d8] = 0xf9;
            agariTable[0xdb490] = 0xf9;
            agariTable[0x6da010080] = 0xf9;
            agariTable[0x3600000000] = 0x68;
            agariTable[0x6d8412000] = 0xf9;
            agariTable[0x6c0090000] = 0xa9;
            agariTable[0x4004db400] = 0xf9;
            agariTable[0x80201b600] = 0xf9;
            agariTable[0x36c0002800] = 0xf9;
            agariTable[0x12000000] = 0x45;
            agariTable[0x20106c0] = 0xa9;
            agariTable[0x20136d0] = 0xf9;
            agariTable[0x1b610800] = 0xf9;
            agariTable[0x1b620400] = 0xf9;
            agariTable[0x36c0100080] = 0xf9;
            agariTable[0x3010] = 0x57;
            agariTable[0x20004806d8] = 0xf9;
            agariTable[0x1004036c0] = 0xf9;
            agariTable[0x40db400] = 0xf9;
            agariTable[0xd8800] = 0xa9;
            agariTable[0x4da000000] = 0xa9;
            agariTable[0x20004000d8] = 0xa9;
            agariTable[0x6c0400010] = 0xa9;
            agariTable[0x36c0080800] = 0xf9;
            agariTable[0x36c2480000] = 0xf9;
            agariTable[0x1000000d8] = 0xa9;
            agariTable[0x409b600] = 0xf9;
            agariTable[0x20000db800] = 0xf9;
            agariTable[0x1036c0400] = 0xf9;
            agariTable[0x41b682000] = 0xf9;
            agariTable[0xdb410400] = 0xf9;
            agariTable[0x6d2000000] = 0xa9;
            agariTable[0x36c2002010] = 0xf9;
            agariTable[0x800836d0] = 0xf9;
            agariTable[0x40009b000] = 0xa9;
            agariTable[0x236c0080] = 0xf9;
            agariTable[0x9b602080] = 0xf9;
            agariTable[0x9b080000] = 0xa9;
            agariTable[0x4db090000] = 0xf9;
            agariTable[0x48009b600] = 0xf9;
            agariTable[0x100126d8] = 0xf9;
            agariTable[0x1241b600] = 0xf9;
            agariTable[0x24006da000] = 0xf9;
            agariTable[0x6d8082010] = 0xf9;
            agariTable[0x480] = 0x45;
            agariTable[0x4800d8] = 0xa9;
            agariTable[0x36c0420] = 0xf9;
            agariTable[0x9b000010] = 0xa9;
            agariTable[0x610000] = 0x57;
            agariTable[0x20d8000400] = 0xa9;
            agariTable[0x36c0482000] = 0xf9;
            agariTable[0x1b004000] = 0xa9;
            agariTable[0x106d8020] = 0xf9;
            agariTable[0x9b680010] = 0xf9;
            agariTable[0x36d8] = 0xc8;
            agariTable[0x6d8080090] = 0xf9;
            agariTable[0x6d8802000] = 0xf9;
            agariTable[0x220036c0] = 0xf9;
            agariTable[0x201b600090] = 0xf9;
            agariTable[0x209b680000] = 0xf9;
            agariTable[0x26c0400000] = 0xa9;
            agariTable[0x4db100] = 0xf9;
            agariTable[0x800db800] = 0xf9;
            agariTable[0x201b690000] = 0xf9;
            agariTable[0x20db410000] = 0xf9;
            agariTable[0x92492000] = 0xe5;
            agariTable[0x3000400] = 0x57;
            agariTable[0x9b600410] = 0xf9;
            agariTable[0x6dc010000] = 0xf9;
            agariTable[0x36d0402000] = 0xf9;
            agariTable[0x200000] = 0x13;
            agariTable[0x2009b600] = 0xf9;
            agariTable[0x20800db080] = 0xf9;
            agariTable[0x20036c2400] = 0xf9;
            agariTable[0x41b600800] = 0xf9;
            agariTable[0x9b612000] = 0xf9;
            agariTable[0x1b6a0] = 0xf9;
            agariTable[0x8db400] = 0xf9;
            agariTable[0x4006d8480] = 0xf9;
            agariTable[0x4906d8000] = 0xf9;
            agariTable[0x11b680000] = 0xf9;
            agariTable[0x36c2000480] = 0xf9;
            agariTable[0x804036d0] = 0xf9;
            agariTable[0x8001b620] = 0xf9;
            agariTable[0x41b690] = 0xf9;
            agariTable[0xdb0000a0] = 0xf9;
            agariTable[0x20d8400000] = 0xa9;
            agariTable[0x6d8402400] = 0xf9;
            agariTable[0x36d0480000] = 0xf9;
            agariTable[0x24004006d8] = 0xf9;
            agariTable[0xd8002400] = 0xa9;
            agariTable[0x36c0080090] = 0xf9;
            agariTable[0x36c0000880] = 0xf9;
            agariTable[0x36c0400480] = 0xf9;
            agariTable[0x200d8000] = 0xa9;
            agariTable[0x9249240] = 0x81;
            agariTable[0x18000080] = 0x57;
            agariTable[0x1249248] = 0x81;
            agariTable[0x4000036e0] = 0xf9;
            agariTable[0x36d0800] = 0xf9;
            agariTable[0x209b602000] = 0xf9;
            agariTable[0x126c0] = 0xa9;
            agariTable[0x2010003600] = 0xa9;
            agariTable[0x136e0000] = 0xf9;
            agariTable[0x20836d0000] = 0xf9;
            agariTable[0x6d8410010] = 0xf9;
            agariTable[0x6dc002000] = 0xf9;
            agariTable[0xa36c0] = 0xf9;
            agariTable[0x483600000] = 0xa9;
            agariTable[0x11b610000] = 0xf9;
            agariTable[0x36d4000000] = 0xf9;
            agariTable[0x800100d8] = 0xa9;
            agariTable[0x936d0000] = 0xf9;
            agariTable[0x9249000] = 0x61;
            agariTable[0x4db012000] = 0xf9;
            agariTable[0x36c0400020] = 0xf9;
            agariTable[0x1249249240] = 0xb1;
            agariTable[0x2003610] = 0xa9;
            agariTable[0x2000403600] = 0xa9;
            agariTable[0x1001b620] = 0xf9;
            agariTable[0x10001b000] = 0xa9;
            agariTable[0x6d2000] = 0xa9;
            agariTable[0x9b602400] = 0xf9;
            agariTable[0x41b680400] = 0xf9;
            agariTable[0x20c0000] = 0x57;
            agariTable[0x4000000] = 0x42;
            agariTable[0x120136c0] = 0xf9;
            agariTable[0x50001b600] = 0xf9;
            agariTable[0x201001b000] = 0xa9;
            agariTable[0x36c0110000] = 0xf9;
            agariTable[0x820006c0] = 0xa9;
            agariTable[0x3600090000] = 0xa9;
            agariTable[0x100100d8] = 0xa9;
            agariTable[0x4001036c0] = 0xf9;
            agariTable[0x4020836c0] = 0xf9;
            agariTable[0x906d8010] = 0xf9;
            agariTable[0x249240000] = 0x61;
            agariTable[0x20000826d8] = 0xf9;
            agariTable[0x24db080000] = 0xf9;
            agariTable[0x36c0082010] = 0xf9;
            agariTable[0x36c0090080] = 0xf9;
            agariTable[0x2000018] = 0x57;
            agariTable[0x80009b600] = 0xf9;
            agariTable[0x41201b600] = 0xf9;
            agariTable[0x4100db400] = 0xf9;
            agariTable[0x806d8020] = 0xf9;
            agariTable[0x836c0800] = 0xf9;
            agariTable[0x2400000] = 0x45;
            agariTable[0x8db080] = 0xf9;
            agariTable[0x136d2000] = 0xf9;
            agariTable[0x23600000] = 0xa9;
            agariTable[0x2018000000] = 0x57;
            agariTable[0x20026da000] = 0xf9;
            agariTable[0x83600400] = 0xa9;
            agariTable[0x8000db400] = 0xf9;
            agariTable[0x81b600010] = 0xf9;
            agariTable[0x680000000] = 0x57;
            agariTable[0x1b602090] = 0xf9;
            agariTable[0x41b600090] = 0xf9;
            agariTable[0x3610000400] = 0xa9;
            agariTable[0x3600012000] = 0xa9;
            agariTable[0x1a000] = 0x57;
            agariTable[0x13000] = 0x57;
            agariTable[0x3000002000] = 0x57;
            agariTable[0x4004d8000] = 0xa9;
            agariTable[0x6d8010480] = 0xf9;
            agariTable[0x6e0000000] = 0xa9;
            agariTable[0x36d2000400] = 0xf9;
            agariTable[0x49248] = 0x61;
            agariTable[0x4106da000] = 0xf9;
            agariTable[0x1b600500] = 0xf9;
            agariTable[0xdb002020] = 0xf9;
            agariTable[0x36e0000010] = 0xf9;
            agariTable[0x808db000] = 0xf9;
            agariTable[0x20004d8] = 0xa9;
            agariTable[0xd8100] = 0xa9;
            agariTable[0x3682000] = 0xa9;
            agariTable[0x36e0000080] = 0xf9;
            agariTable[0x1b600] = 0x98;
            agariTable[0x5036c0] = 0xf9;
            agariTable[0x8000836c0] = 0xf9;
            agariTable[0x201b680080] = 0xf9;
            agariTable[0x6d8000420] = 0xf9;
            agariTable[0x3010000000] = 0x57;
            agariTable[0x418] = 0x57;
            agariTable[0x200049b600] = 0xf9;
            agariTable[0x8401b600] = 0xf9;
            agariTable[0x209b000] = 0xa9;
            agariTable[0x36d0410] = 0xf9;
            agariTable[0xdb400100] = 0xf9;
            agariTable[0x36d0010080] = 0xf9;
            agariTable[0x6c0] = 0x68;
            agariTable[0x100000000] = 0x42;
            agariTable[0x808006d8] = 0xf9;
            agariTable[0x1036c2000] = 0xf9;
            agariTable[0x49b600010] = 0xf9;
            agariTable[0xdb100400] = 0xf9;
            agariTable[0x36c0020010] = 0xf9;
            agariTable[0x600080000] = 0x57;
            agariTable[0x4036d0080] = 0xf9;
            agariTable[0x81b000000] = 0xa9;
            agariTable[0x21000006d8] = 0xf9;
            agariTable[0x4db000410] = 0xf9;
            agariTable[0x4020db010] = 0xf9;
            agariTable[0x200401b600] = 0xf9;
            agariTable[0x9b680400] = 0xf9;
            agariTable[0x20db000020] = 0xf9;
            agariTable[0x40041b610] = 0xf9;
            agariTable[0xc0002000] = 0x57;
            agariTable[0x4100db010] = 0xf9;
            agariTable[0x26d0000] = 0xa9;
            agariTable[0x4db010010] = 0xf9;
            agariTable[0x3610080000] = 0xa9;
            agariTable[0x4106c0] = 0xa9;
            agariTable[0x40036d0] = 0xf9;
            agariTable[0x26d8100] = 0xf9;
            agariTable[0xdb0a0000] = 0xf9;
            agariTable[0x36c4000400] = 0xf9;
            agariTable[0x8020006d8] = 0xf9;
            agariTable[0x1000db400] = 0xf9;
            agariTable[0x20006c0080] = 0xa9;
            agariTable[0x20000136d0] = 0xf9;
            agariTable[0x200201b680] = 0xf9;
            agariTable[0x3000000010] = 0x57;
            agariTable[0x836c2400] = 0xf9;
            agariTable[0x136d0400] = 0xf9;
            agariTable[0x1b600880] = 0xf9;
            agariTable[0x20db100000] = 0xf9;
            agariTable[0x36c0010410] = 0xf9;
            agariTable[0x36c0402080] = 0xf9;
            agariTable[0x5000006d8] = 0xf9;
            agariTable[0x146d8000] = 0xf9;
            agariTable[0x13600010] = 0xa9;
            agariTable[0x6c0002080] = 0xa9;
            agariTable[0x36c0400090] = 0xf9;
            agariTable[0x1001b700] = 0xf9;
            agariTable[0x104d8] = 0xa9;
            agariTable[0x1000d8] = 0xa9;
            agariTable[0x4800db400] = 0xf9;
            agariTable[0x4006d8100] = 0xf9;
            agariTable[0x6d8090400] = 0xf9;
            agariTable[0x26c0000010] = 0xa9;
            agariTable[0x36d0000800] = 0xf9;
            agariTable[0x490] = 0x65;
            agariTable[0x936c0010] = 0xf9;
            agariTable[0x49b600400] = 0xf9;
            agariTable[0x800d8400] = 0xa9;
            agariTable[0x20036e0] = 0xf9;
            agariTable[0x800db100] = 0xf9;
            agariTable[0x9b610400] = 0xf9;
            agariTable[0x4000806c0] = 0xa9;
            agariTable[0x20806d8400] = 0xf9;
            agariTable[0x836c4000] = 0xf9;
            agariTable[0x6c0000100] = 0xa9;
            agariTable[0x36c0010100] = 0xf9;
            agariTable[0x800026c0] = 0xa9;
            agariTable[0x20000db480] = 0xf9;
            agariTable[0x20008db000] = 0xf9;
            agariTable[0x1b480000] = 0xa9;
            agariTable[0x6da012000] = 0xf9;
            agariTable[0x403000] = 0x57;
            agariTable[0x36c0000000] = 0x98;
            agariTable[0x2018000] = 0x57;
            agariTable[0x1041b610] = 0xf9;
            agariTable[0x4026da000] = 0xf9;
            agariTable[0x201b612000] = 0xf9;
            agariTable[0x201b400000] = 0xa9;
            agariTable[0x92400] = 0x85;
            agariTable[0x4020036d0] = 0xf9;
            agariTable[0x1004db000] = 0xf9;
            agariTable[0x4000020d8] = 0xa9;
            agariTable[0x36c0800400] = 0xf9;
            agariTable[0x3680010000] = 0xa9;
            agariTable[0x20db010400] = 0xf9;
            agariTable[0x10013600] = 0xa9;
            agariTable[0x20820db000] = 0xf9;
            agariTable[0x24100db000] = 0xf9;
            agariTable[0x136c0800] = 0xf9;
            agariTable[0x9b602010] = 0xf9;
            agariTable[0x100836d0] = 0xf9;
            agariTable[0x80041b600] = 0xf9;
            agariTable[0x36c0080020] = 0xf9;
            agariTable[0x3080000] = 0x57;
            agariTable[0x18000000] = 0x36;
            agariTable[0x200000d8] = 0xa9;
            agariTable[0x200026d8] = 0xf9;
            agariTable[0x801006d8] = 0xf9;
            agariTable[0x46c0000] = 0xa9;
            agariTable[0x41b620000] = 0xf9;
            agariTable[0x492400000] = 0xa5;
            agariTable[0x806d0000] = 0xa9;
            agariTable[0x4036c0100] = 0xf9;
            agariTable[0x1b614000] = 0xf9;
            agariTable[0xdb010480] = 0xf9;
            agariTable[0x6d80000a0] = 0xf9;
            agariTable[0x3600000410] = 0xa9;
            agariTable[0x1b410] = 0xa9;
            agariTable[0xda010] = 0xa9;
            agariTable[0x83600010] = 0xa9;
            agariTable[0x36c2402000] = 0xf9;
            agariTable[0x20104db000] = 0xf9;
            agariTable[0x4100d8] = 0xa9;
            agariTable[0x4800000d8] = 0xa9;
            agariTable[0x120006c0] = 0xa9;
            agariTable[0x4236c0] = 0xf9;
            agariTable[0x10001b610] = 0xf9;
            agariTable[0x36c2010080] = 0xf9;
            agariTable[0x49249200] = 0x81;
            agariTable[0x4136c0400] = 0xf9;
            agariTable[0x20936c0000] = 0xf9;
            agariTable[0x48001b000] = 0xa9;
            agariTable[0x6c0002010] = 0xa9;
            agariTable[0x418000] = 0x57;
            agariTable[0x36c0082400] = 0xf9;
            agariTable[0x41b602080] = 0xf9;
            agariTable[0x40201b610] = 0xf9;
            agariTable[0x4db020000] = 0xf9;
            agariTable[0x20820006d8] = 0xf9;
            agariTable[0x28006d8000] = 0xf9;
            agariTable[0x36c2400400] = 0xf9;
            agariTable[0x8001b690] = 0xf9;
            agariTable[0x100046d8] = 0xf9;
            agariTable[0x20936c0] = 0xf9;
            agariTable[0x4036c2010] = 0xf9;
            agariTable[0x836e0000] = 0xf9;
            agariTable[0x1b000100] = 0xa9;
            agariTable[0x249200000] = 0x51;
            agariTable[0x1b604400] = 0xf9;
            agariTable[0x36db000] = 0xf8;
            agariTable[0x4006d0] = 0xa9;
            agariTable[0x81b610] = 0xf9;
            agariTable[0x20da000] = 0xa9;
            agariTable[0x236c0010] = 0xf9;
            agariTable[0x1b002010] = 0xa9;
            agariTable[0xdb090080] = 0xf9;
            agariTable[0x1249000000] = 0x51;
            agariTable[0x4000db100] = 0xf9;
            agariTable[0x1b680020] = 0xf9;
            agariTable[0x820000d8] = 0xa9;
            agariTable[0x120036d0] = 0xf9;
            agariTable[0x400013600] = 0xa9;
            agariTable[0x806d8480] = 0xf9;
            agariTable[0x4136c2000] = 0xf9;
            agariTable[0x20db400080] = 0xf9;
            agariTable[0x804026d8] = 0xf9;
            agariTable[0x104026d8] = 0xf9;
            agariTable[0x20006c0010] = 0xa9;
            agariTable[0x6d8000880] = 0xf9;
            agariTable[0x26d8004000] = 0xf9;
            agariTable[0x36c0022000] = 0xf9;
            agariTable[0x80018] = 0x57;
            agariTable[0x26d8100000] = 0xf9;
            agariTable[0x6c0480000] = 0xa9;
            agariTable[0x6c2010] = 0xa9;
            agariTable[0x20000006d0] = 0xa9;
            agariTable[0x40009b680] = 0xf9;
            agariTable[0x4db410] = 0xf9;
            agariTable[0x4db480] = 0xf9;
            agariTable[0xdc000] = 0xa9;
            agariTable[0x20000da000] = 0xa9;
            agariTable[0x36c0004010] = 0xf9;
            agariTable[0x80003000] = 0x57;
            agariTable[0x204db000] = 0xf9;
            agariTable[0xdb000500] = 0xf9;
            agariTable[0x1a000000] = 0x57;
            agariTable[0x6da480] = 0xf9;
            agariTable[0x6dc080] = 0xf9;
            agariTable[0x26d8020000] = 0xf9;
            agariTable[0x24000026d8] = 0xf9;
            agariTable[0x24036c2000] = 0xf9;
            agariTable[0x36c2000090] = 0xf9;
            agariTable[0x4008db000] = 0xf9;
            agariTable[0xdb082400] = 0xf9;
            agariTable[0x36c0012010] = 0xf9;
            agariTable[0x12480] = 0x85;
            agariTable[0x3600400080] = 0xa9;
            agariTable[0x20100db010] = 0xf9;
            agariTable[0x6c2010000] = 0xa9;
            agariTable[0x3682000000] = 0xa9;
            agariTable[0x20db400010] = 0xf9;
            agariTable[0x20db410] = 0xf9;
            agariTable[0x1b620080] = 0xf9;
            agariTable[0x201b700000] = 0xf9;
            agariTable[0x20c0000000] = 0x57;
            agariTable[0x104036d0] = 0xf9;
            agariTable[0x24106d8000] = 0xf9;
            agariTable[0x13600400] = 0xa9;
            agariTable[0xdb020010] = 0xf9;
            agariTable[0xdb002480] = 0xf9;
            agariTable[0x26d8402000] = 0xf9;
            agariTable[0x6d0000010] = 0xa9;
            agariTable[0xc2000000] = 0x57;
            agariTable[0x201b610010] = 0xf9;
            agariTable[0x241b602000] = 0xf9;
            agariTable[0xdc000000] = 0xa9;
            agariTable[0x26d8000090] = 0xf9;
            agariTable[0x900836c0] = 0xf9;
            agariTable[0x240201b600] = 0xf9;
            agariTable[0x106d8480] = 0xf9;
            agariTable[0x1b602410] = 0xf9;
            agariTable[0x20d8002000] = 0xa9;
            agariTable[0x8009b610] = 0xf9;
            agariTable[0x18000400] = 0x57;
            agariTable[0x806d8800] = 0xf9;
            agariTable[0x36c4400000] = 0xf9;
            agariTable[0x80] = 0x24;
            agariTable[0x49240] = 0x51;
            agariTable[0x4104006d8] = 0xf9;
            agariTable[0x8020036c0] = 0xf9;
            agariTable[0x8000d8000] = 0xa9;
            agariTable[0x20026d8400] = 0xf9;
            agariTable[0x36c2800] = 0xf9;
            agariTable[0x81b680000] = 0xf9;
            agariTable[0x40000000] = 0x13;
            agariTable[0x36c2400010] = 0xf9;
            agariTable[0x36c0084000] = 0xf9;
            agariTable[0xdb800400] = 0xf9;
            agariTable[0x24036d0] = 0xf9;
            agariTable[0x4800db080] = 0xf9;
            agariTable[0x20806d8010] = 0xf9;
            agariTable[0x4006da080] = 0xf9;
            agariTable[0x36c00a0] = 0xf9;
            agariTable[0x20000036e0] = 0xf9;
            agariTable[0x1136c0] = 0xf9;
            agariTable[0x24006d8080] = 0xf9;
            agariTable[0x136c0410] = 0xf9;
            agariTable[0x36c0500] = 0xf9;
            agariTable[0x6dc000080] = 0xf9;
            agariTable[0x2600000] = 0x57;
            agariTable[0x4020db080] = 0xf9;
            agariTable[0x24db400000] = 0xf9;
            agariTable[0x6da000410] = 0xf9;
            agariTable[0x4004db010] = 0xf9;
            agariTable[0x136c0020] = 0xf9;
            agariTable[0x3600000480] = 0xa9;
            agariTable[0xa001b600] = 0xf9;
            agariTable[0x441b600] = 0xf9;
            agariTable[0xd8480] = 0xa9;
            agariTable[0x6d8500] = 0xf9;
            agariTable[0xdb102000] = 0xf9;
            agariTable[0x6d8020080] = 0xf9;
            agariTable[0x3600402000] = 0xa9;
            agariTable[0x800806c0] = 0xa9;
            agariTable[0x4000db090] = 0xf9;
            agariTable[0x20000000] = 0x42;
            agariTable[0x18000] = 0x36;
            agariTable[0x8800006d8] = 0xf9;
            agariTable[0x4000136d0] = 0xf9;
            agariTable[0x820136c0] = 0xf9;
            agariTable[0x4000100d8] = 0xa9;
            agariTable[0x8000d8] = 0xa9;
            agariTable[0x200806d8] = 0xf9;
            agariTable[0x36d0090] = 0xf9;
            agariTable[0x26d8000020] = 0xf9;
            agariTable[0x6c0000410] = 0xa9;
            agariTable[0x3600000] = 0x68;
            agariTable[0x24020036c0] = 0xf9;
            agariTable[0xdb010090] = 0xf9;
            agariTable[0xd8000100] = 0xa9;
            agariTable[0x36c2002080] = 0xf9;
            agariTable[0x36d2400000] = 0xf9;
            agariTable[0xc0000010] = 0x57;
            agariTable[0xdb880000] = 0xf9;
            agariTable[0x6d8004080] = 0xf9;
            agariTable[0x6d0010000] = 0xa9;
            agariTable[0x3610000010] = 0xa9;
            agariTable[0x804006c0] = 0xa9;
            agariTable[0x8041b680] = 0xf9;
            agariTable[0x36d0480] = 0xf9;
            agariTable[0x201b600100] = 0xf9;
            agariTable[0x36d0002080] = 0xf9;
            agariTable[0x4040006d8] = 0xf9;
            agariTable[0x98] = 0x57;
            agariTable[0x4800106d8] = 0xf9;
            agariTable[0x209b600400] = 0xf9;
            agariTable[0x92400000] = 0x85;
            agariTable[0x1000136c0] = 0xf9;
            agariTable[0x83610] = 0xa9;
            agariTable[0x4d8400] = 0xa9;
            agariTable[0x36d0020] = 0xf9;
            agariTable[0x1b600810] = 0xf9;
            agariTable[0x36c0402400] = 0xf9;
            agariTable[0x24100006d8] = 0xf9;
            agariTable[0x20000d8080] = 0xa9;
            agariTable[0x1b400400] = 0xa9;
            agariTable[0x1b600000] = 0x98;
            agariTable[0x8100006d8] = 0xf9;
            agariTable[0x41b700000] = 0xf9;
            agariTable[0x41b002000] = 0xa9;
            agariTable[0x26d0000000] = 0xa9;
            agariTable[0x1100006d8] = 0xf9;
            agariTable[0x23600] = 0xa9;
            agariTable[0x1011b600] = 0xf9;
            agariTable[0x100db480] = 0xf9;
            agariTable[0x2000003610] = 0xa9;
            agariTable[0x400000] = 0x24;
            agariTable[0x900036d0] = 0xf9;
            agariTable[0x2000003680] = 0xa9;
            agariTable[0x80083600] = 0xa9;
            agariTable[0x249b600] = 0xf9;
            agariTable[0x6c0480] = 0xa9;
            agariTable[0x8036c0080] = 0xf9;
            agariTable[0x249249200] = 0x91;
            agariTable[0x241b610000] = 0xf9;
            agariTable[0x6c0000020] = 0xa9;
            agariTable[0x11b600010] = 0xf9;
            agariTable[0x1036d0000] = 0xf9;
            agariTable[0x4004036d0] = 0xf9;
            agariTable[0x1b6a0000] = 0xf9;
            agariTable[0x6da400010] = 0xf9;
            agariTable[0x36c0400410] = 0xf9;
            agariTable[0x1041b000] = 0xa9;
            agariTable[0x20106d8080] = 0xf9;
            agariTable[0x1036c0080] = 0xf9;
            agariTable[0x3600410] = 0xa9;
            agariTable[0x9b690000] = 0xf9;
            agariTable[0x24db000400] = 0xf9;
            agariTable[0x4001006d8] = 0xf9;
            agariTable[0x26d8010400] = 0xf9;
            agariTable[0x6da004000] = 0xf9;
            agariTable[0x20004026d8] = 0xf9;
            agariTable[0x3010000] = 0x57;
            agariTable[0x4000d8400] = 0xa9;
            agariTable[0x6da002080] = 0xf9;
            agariTable[0x800c0000] = 0x57;
            agariTable[0x800000c0] = 0x57;
            agariTable[0x20206d8] = 0xf9;
            agariTable[0x20004006c0] = 0xa9;
            agariTable[0x400403600] = 0xa9;
            agariTable[0x6c0800000] = 0xa9;
            agariTable[0x1249249248] = 0xc1;
            agariTable[0x800800d8] = 0xa9;
            agariTable[0x40001b700] = 0xf9;
            agariTable[0x36e0010000] = 0xf9;
            agariTable[0x40001b620] = 0xf9;
            agariTable[0x36d0004000] = 0xf9;
            agariTable[0x4200006d8] = 0xf9;
            agariTable[0x4046d8] = 0xf9;
            agariTable[0x210001b600] = 0xf9;
            agariTable[0x20036c0020] = 0xf9;
            agariTable[0x36d2000010] = 0xf9;
            agariTable[0x10000000] = 0x24;
            agariTable[0x20000236c0] = 0xf9;
            agariTable[0x140db000] = 0xf9;
            agariTable[0x20800806d8] = 0xf9;
            agariTable[0x1020036c0] = 0xf9;
            agariTable[0x400003610] = 0xa9;
            agariTable[0x820db400] = 0xf9;
            agariTable[0x36d4000] = 0xf9;
            agariTable[0x8036c2000] = 0xf9;
            agariTable[0xdb004080] = 0xf9;
            agariTable[0x20db004000] = 0xf9;
            agariTable[0x20000026c0] = 0xa9;
            agariTable[0x4800836c0] = 0xf9;
            agariTable[0x49b610] = 0xf9;
            agariTable[0x9201b600] = 0xf9;
            agariTable[0x4206d8000] = 0xf9;
            agariTable[0x836d0010] = 0xf9;
            agariTable[0xa36c0000] = 0xf9;
            agariTable[0x201b602400] = 0xf9;
            agariTable[0x20100000d8] = 0xa9;
            agariTable[0x20db402000] = 0xf9;
            agariTable[0x2492480000] = 0xe5;
            agariTable[0x20036c2080] = 0xf9;
            agariTable[0x20036d2000] = 0xf9;
            agariTable[0x20db480000] = 0xf9;
            agariTable[0x26d8002400] = 0xf9;
            agariTable[0x6da082000] = 0xf9;
            agariTable[0x3600400400] = 0xa9;
            agariTable[0x9248] = 0x51;
            agariTable[0x24806d8] = 0xf9;
            agariTable[0x20040036c0] = 0xf9;
            agariTable[0x4006c0010] = 0xa9;
            agariTable[0x4d8000400] = 0xa9;
            agariTable[0x6d8400090] = 0xf9;
            agariTable[0x6da400080] = 0xf9;
            agariTable[0x3000010] = 0x57;
            agariTable[0x1009b610] = 0xf9;
            agariTable[0x200201b000] = 0xa9;
            agariTable[0x1000000000] = 0x13;
            agariTable[0x600000400] = 0x57;
            agariTable[0x1201b610] = 0xf9;
            agariTable[0x249249240] = 0xa1;
            agariTable[0xdb800080] = 0xf9;
            agariTable[0x6d8420000] = 0xf9;
            agariTable[0x8836c0000] = 0xf9;
            agariTable[0xd8020] = 0xa9;
            agariTable[0x106d8100] = 0xf9;
            agariTable[0x20126d8] = 0xf9;
            agariTable[0x4004d8] = 0xa9;
            agariTable[0x806d0] = 0xa9;
            agariTable[0x20004836c0] = 0xf9;
            agariTable[0x4006d8020] = 0xf9;
            agariTable[0x36c4080] = 0xf9;
            agariTable[0x236c0400] = 0xf9;
            agariTable[0x1b680410] = 0xf9;
            agariTable[0x36c0] = 0x98;
            agariTable[0x20036c4000] = 0xf9;
            agariTable[0xdb480400] = 0xf9;
            agariTable[0x36d0000090] = 0xf9;
            agariTable[0x49b000] = 0xa9;
            agariTable[0x20000836d0] = 0xf9;
            agariTable[0x8036d0] = 0xf9;
            agariTable[0x80013600] = 0xa9;
            agariTable[0x41b700] = 0xf9;
            agariTable[0x803600000] = 0xa9;
            agariTable[0x4d8010000] = 0xa9;
            agariTable[0x6c0012000] = 0xa9;
            agariTable[0x20040006d8] = 0xf9;
            agariTable[0xdb012080] = 0xf9;
            agariTable[0x100800d8] = 0xa9;
            agariTable[0x20026d8010] = 0xf9;
            agariTable[0x20000206d8] = 0xf9;
            agariTable[0x106d0] = 0xa9;
            agariTable[0xdb404000] = 0xf9;
            agariTable[0x20da000000] = 0xa9;
            agariTable[0x24020006d8] = 0xf9;
            agariTable[0x44036c0] = 0xf9;
            agariTable[0x40001b690] = 0xf9;
            agariTable[0xdb810] = 0xf9;
            agariTable[0x201b000080] = 0xa9;
            agariTable[0xd8000020] = 0xa9;
            agariTable[0x600080] = 0x57;
            agariTable[0x41b010] = 0xa9;
            agariTable[0x9b604000] = 0xf9;
            agariTable[0x6c0010400] = 0xa9;
            agariTable[0x6c0400400] = 0xa9;
            agariTable[0xc0010000] = 0x57;
            agariTable[0x20004db400] = 0xf9;
            agariTable[0x806d8410] = 0xf9;
            agariTable[0xdb090400] = 0xf9;
            agariTable[0x208041b600] = 0xf9;
            agariTable[0x20040db000] = 0xf9;
            agariTable[0x4120db000] = 0xf9;
            agariTable[0x826d8080] = 0xf9;
            agariTable[0x211b600000] = 0xf9;
            agariTable[0x6d8000110] = 0xf9;
            agariTable[0x26d8410000] = 0xf9;
            agariTable[0x6d0002000] = 0xa9;
            agariTable[0x4008036c0] = 0xf9;
            agariTable[0x36c0500000] = 0xf9;
            agariTable[0x4006c0080] = 0xa9;
            agariTable[0x6d8490000] = 0xf9;
            agariTable[0x18080000] = 0x57;
            agariTable[0x6d8082080] = 0xf9;
            agariTable[0x6d8084000] = 0xf9;
            agariTable[0x3600800000] = 0xa9;
            agariTable[0x820d8] = 0xa9;
            agariTable[0x820106d8] = 0xf9;
            agariTable[0x1001b400] = 0xa9;
            agariTable[0x20236c0000] = 0xf9;
            agariTable[0x10018000] = 0x57;
            agariTable[0x49001b600] = 0xf9;
            agariTable[0x8100036c0] = 0xf9;
            agariTable[0x4000db020] = 0xf9;
            agariTable[0xdb082080] = 0xf9;
            agariTable[0xd8090000] = 0xa9;
            agariTable[0x8000000d8] = 0xa9;
            agariTable[0x4100000d8] = 0xa9;
            agariTable[0x836d0080] = 0xf9;
            agariTable[0xdb020080] = 0xf9;
            agariTable[0xdb000000] = 0x98;
            agariTable[0x936d0] = 0xf9;
            agariTable[0x1001b010] = 0xa9;
            agariTable[0x4136c0010] = 0xf9;
            agariTable[0xda000080] = 0xa9;
            agariTable[0x26c2000000] = 0xa9;
            agariTable[0x249248] = 0x71;
            agariTable[0xd8410] = 0xa9;
            agariTable[0x6d8490] = 0xf9;
            agariTable[0x403600080] = 0xa9;
            agariTable[0x4db010400] = 0xf9;
            agariTable[0x6d8410400] = 0xf9;
            agariTable[0x900106d8] = 0xf9;
            agariTable[0x26da010] = 0xf9;
            agariTable[0x36c2480] = 0xf9;
            agariTable[0x1b710000] = 0xf9;
            agariTable[0xdb010100] = 0xf9;
            agariTable[0x4db000800] = 0xf9;
            agariTable[0x400000600] = 0x57;
            agariTable[0x20006d8480] = 0xf9;
            agariTable[0x13602000] = 0xa9;
            agariTable[0x1b680480] = 0xf9;
            agariTable[0x3600082000] = 0xa9;
            agariTable[0x20008036c0] = 0xf9;
            agariTable[0x4000126d8] = 0xf9;
            agariTable[0x6da090000] = 0xf9;
            agariTable[0x36c0000] = 0x98;
            agariTable[0x20020036d0] = 0xf9;
            agariTable[0x4da000] = 0xa9;
            agariTable[0x1006d8400] = 0xf9;
            agariTable[0x1b610020] = 0xf9;
            agariTable[0x9b600090] = 0xf9;
            agariTable[0x1b622000] = 0xf9;
            agariTable[0x9b010000] = 0xa9;
            agariTable[0x40000] = 0x13;
            agariTable[0x36e0080] = 0xf9;
            agariTable[0xdb000810] = 0xf9;
            agariTable[0x8db080000] = 0xf9;
            agariTable[0x906c0000] = 0xa9;
            agariTable[0x4d8080] = 0xa9;
            agariTable[0xd8002080] = 0xa9;
            agariTable[0x24000836c0] = 0xf9;
            agariTable[0x24db080] = 0xf9;
            agariTable[0x106d8800] = 0xf9;
            agariTable[0x36d2080] = 0xf9;
            agariTable[0x10083600] = 0xa9;
            agariTable[0x200041b000] = 0xa9;
            agariTable[0x6d80a0] = 0xf9;
            agariTable[0x20804006d8] = 0xf9;
            agariTable[0x20100d8000] = 0xa9;
            agariTable[0x4006d8090] = 0xf9;
            agariTable[0x1009b680] = 0xf9;
            agariTable[0x10003000] = 0x57;
            agariTable[0x236d0] = 0xf9;
            agariTable[0x403600400] = 0xa9;
            agariTable[0x18] = 0x36;
            agariTable[0xda080] = 0xa9;
            agariTable[0x36d0082000] = 0xf9;
            agariTable[0x4000db410] = 0xf9;
            agariTable[0x81b000] = 0xa9;
            agariTable[0x904db000] = 0xf9;
            agariTable[0x6c4000] = 0xa9;
            agariTable[0x3000000080] = 0x57;
            agariTable[0x11b000000] = 0xa9;
            agariTable[0x4026c0] = 0xa9;
            agariTable[0xdb010410] = 0xf9;
            agariTable[0xdb084000] = 0xf9;
            agariTable[0x36e0000400] = 0xf9;
            agariTable[0x100826d8] = 0xf9;
            agariTable[0x106da080] = 0xf9;
            agariTable[0x1b680090] = 0xf9;
            agariTable[0x1b602100] = 0xf9;
            agariTable[0x6d8082400] = 0xf9;
            agariTable[0x1000db080] = 0xf9;
            agariTable[0x800106c0] = 0xa9;
            agariTable[0x24000006c0] = 0xa9;
            agariTable[0x104836c0] = 0xf9;
            agariTable[0x108036c0] = 0xf9;
            agariTable[0x126c0000] = 0xa9;
            agariTable[0x6da800000] = 0xf9;
            agariTable[0x36c2020000] = 0xf9;
            agariTable[0x100020d8] = 0xa9;
            agariTable[0x20000] = 0x42;
            agariTable[0x104106d8] = 0xf9;
            agariTable[0x840db000] = 0xf9;
            agariTable[0x480000] = 0x45;
            agariTable[0x20836c0400] = 0xf9;
            agariTable[0x403610000] = 0xa9;
            agariTable[0x41b610400] = 0xf9;
            agariTable[0x5000036c0] = 0xf9;
            agariTable[0x800da000] = 0xa9;
            agariTable[0x6d0080] = 0xa9;
            agariTable[0x24036d0000] = 0xf9;
            agariTable[0x3610010] = 0xa9;
            agariTable[0x1b410000] = 0xa9;
            agariTable[0x36d0000410] = 0xf9;
            agariTable[0x20200db000] = 0xf9;
            agariTable[0x20100036d0] = 0xf9;
            agariTable[0x6da100] = 0xf9;
            agariTable[0x41b000080] = 0xa9;
            agariTable[0x209b000000] = 0xa9;
            agariTable[0x6d8022000] = 0xf9;
            agariTable[0x1000] = 0x13;
            agariTable[0x136c4000] = 0xf9;
            agariTable[0x1b600110] = 0xf9;
            agariTable[0x41b602010] = 0xf9;
            agariTable[0x136e0] = 0xf9;
            agariTable[0x20800136c0] = 0xf9;
            agariTable[0x200201b610] = 0xf9;
            agariTable[0x11b000] = 0xa9;
            agariTable[0x41b600100] = 0xf9;
            agariTable[0xd8004000] = 0xa9;
            agariTable[0x36c2000100] = 0xf9;
            agariTable[0x36c4010000] = 0xf9;
            agariTable[0x124006d8] = 0xf9;
            agariTable[0x20800000d8] = 0xa9;
            agariTable[0x26d8480] = 0xf9;
            agariTable[0x136c2010] = 0xf9;
            agariTable[0x936c0400] = 0xf9;
            agariTable[0x6db000] = 0xc8;
            agariTable[0x8000036d0] = 0xf9;
            agariTable[0x4936c0] = 0xf9;
            agariTable[0x1b610480] = 0xf9;
            agariTable[0x6d8480010] = 0xf9;
            agariTable[0x36c4000080] = 0xf9;
            agariTable[0xd8000] = 0x68;
            agariTable[0x1100036c0] = 0xf9;
            agariTable[0x403610] = 0xa9;
            agariTable[0xdb110] = 0xf9;
            agariTable[0x26da400000] = 0xf9;
            agariTable[0x3602002000] = 0xa9;
            agariTable[0x6c0000000] = 0x68;
            agariTable[0x1000806d8] = 0xf9;
            agariTable[0x20800db400] = 0xf9;
            agariTable[0x36c4010] = 0xf9;
            agariTable[0x4836c0080] = 0xf9;
            agariTable[0x6c0100000] = 0xa9;
            agariTable[0x120026d8] = 0xf9;
            agariTable[0x140006d8] = 0xf9;
            agariTable[0x936c2000] = 0xf9;
            agariTable[0x3610010000] = 0xa9;
            agariTable[0x804806d8] = 0xf9;
            agariTable[0x80000600] = 0x57;
            agariTable[0x80018000] = 0x57;
            agariTable[0x200001b400] = 0xa9;
            agariTable[0x4136d0000] = 0xf9;
            agariTable[0xdb400020] = 0xf9;
            agariTable[0x20db010010] = 0xf9;
            agariTable[0x9249248] = 0x91;
            agariTable[0x20006d0] = 0xa9;
            agariTable[0x208001b680] = 0xf9;
            agariTable[0x20006c0400] = 0xa9;
            agariTable[0xdb080480] = 0xf9;
            agariTable[0x26d8012000] = 0xf9;
            agariTable[0x1249200000] = 0x61;
            agariTable[0x28006d8] = 0xf9;
            agariTable[0x20020836c0] = 0xf9;
            agariTable[0x1b000480] = 0xa9;
            agariTable[0x20db000100] = 0xf9;
            agariTable[0x4004106d8] = 0xf9;
            agariTable[0x200241b600] = 0xf9;
            agariTable[0x806d8100] = 0xf9;
            agariTable[0x4006c2000] = 0xa9;
            agariTable[0x20db082000] = 0xf9;
            agariTable[0x36c0410010] = 0xf9;
            agariTable[0x36c0092000] = 0xf9;
            agariTable[0x41b620] = 0xf9;
            agariTable[0x40db010] = 0xf9;
            agariTable[0x1000db010] = 0xf9;
            agariTable[0x6d8012400] = 0xf9;
            agariTable[0x8041b610] = 0xf9;
            agariTable[0x4804036c0] = 0xf9;
            agariTable[0x201b700] = 0xf9;
            agariTable[0x4000db480] = 0xf9;
            agariTable[0x20806c0000] = 0xa9;
            agariTable[0x49b610000] = 0xf9;
            agariTable[0x200006c0] = 0xa9;
            agariTable[0x4836c0010] = 0xf9;
            agariTable[0x13600080] = 0xa9;
            agariTable[0x24020db000] = 0xf9;
            agariTable[0x2001b680] = 0xf9;
            agariTable[0x20db800000] = 0xf9;
            agariTable[0x3000010000] = 0x57;
            agariTable[0xc0000] = 0x36;
            agariTable[0x10000600] = 0x57;
            agariTable[0x8000026d8] = 0xf9;
            agariTable[0x4800006c0] = 0xa9;
            agariTable[0x20d8400] = 0xa9;
            agariTable[0xdb000490] = 0xf9;
            agariTable[0x400] = 0x24;
            agariTable[0x101006d8] = 0xf9;
            agariTable[0x4000836d0] = 0xf9;
            agariTable[0x8011b600] = 0xf9;
            agariTable[0x1100db000] = 0xf9;
            agariTable[0x1006d8080] = 0xf9;
            agariTable[0x20036d0080] = 0xf9;
            agariTable[0x4036d0400] = 0xf9;
            agariTable[0x18400000] = 0x57;
            agariTable[0x36c0410400] = 0xf9;
            agariTable[0x240041b600] = 0xf9;
            agariTable[0x40401b600] = 0xf9;
            agariTable[0x8009b000] = 0xa9;
            agariTable[0x6da020] = 0xf9;
            agariTable[0x1b600420] = 0xf9;
            agariTable[0x1b610100] = 0xf9;
            agariTable[0x4db480000] = 0xf9;
            agariTable[0x10600000] = 0x57;
            agariTable[0x3610002000] = 0xa9;
            agariTable[0x36d0800000] = 0xf9;
            agariTable[0x4db002080] = 0xf9;
            agariTable[0x6c2080000] = 0xa9;
            agariTable[0x8041b000] = 0xa9;
            agariTable[0xc2000] = 0x57;
            agariTable[0x6d8020010] = 0xf9;
            agariTable[0x26d8080010] = 0xf9;
            agariTable[0x100] = 0x42;
            agariTable[0x10600] = 0x57;
            agariTable[0x4100806d8] = 0xf9;
            agariTable[0x836c0100] = 0xf9;
            agariTable[0x6da080400] = 0xf9;
            agariTable[0x36c0102000] = 0xf9;
            agariTable[0x4100106d8] = 0xf9;
            agariTable[0x804106d8] = 0xf9;
            agariTable[0x20804036c0] = 0xf9;
            agariTable[0x83680] = 0xa9;
            agariTable[0x209b600080] = 0xf9;
            agariTable[0x1b400010] = 0xa9;
            agariTable[0x6c0000] = 0x68;
            agariTable[0x51b600] = 0xf9;
            agariTable[0x6d8080480] = 0xf9;
            agariTable[0x36c4000010] = 0xf9;
            agariTable[0x1] = 0x13;
            agariTable[0x10000018] = 0x57;
            agariTable[0x40d8] = 0xa9;
            agariTable[0x9b080] = 0xa9;
            agariTable[0x20046d8000] = 0xf9;
            agariTable[0x9b610010] = 0xf9;
            agariTable[0x3000000000] = 0x32;
            agariTable[0x103600] = 0xa9;
            agariTable[0x40041b680] = 0xf9;
            agariTable[0x8000db080] = 0xf9;
            agariTable[0x6dc010] = 0xf9;
            agariTable[0x3612000] = 0xa9;
            agariTable[0x201b600480] = 0xf9;
            agariTable[0x6da080010] = 0xf9;
            agariTable[0x4800036d0] = 0xf9;
            agariTable[0x36c2080400] = 0xf9;
            agariTable[0x24db010] = 0xf9;
            agariTable[0x20836c2000] = 0xf9;
            agariTable[0x6c0080010] = 0xa9;
            agariTable[0x4000c0000] = 0x57;
            agariTable[0x40106d8] = 0xf9;
            agariTable[0x20020006c0] = 0xa9;
            agariTable[0x80001b000] = 0xa9;
            agariTable[0x1b402000] = 0xa9;
            agariTable[0x4db004000] = 0xf9;
            agariTable[0x26c0002000] = 0xa9;
            agariTable[0x36d2080000] = 0xf9;
            agariTable[0x12480000] = 0x85;
            agariTable[0x6d8080410] = 0xf9;
            agariTable[0x200009b680] = 0xf9;
            agariTable[0x20004106d8] = 0xf9;
            agariTable[0x20900db000] = 0xf9;
            agariTable[0x4020d8000] = 0xa9;
            agariTable[0x4836c0400] = 0xf9;
            agariTable[0x1b6000a0] = 0xf9;
            agariTable[0x6da480000] = 0xf9;
            agariTable[0x4000800d8] = 0xa9;
            agariTable[0x804000d8] = 0xa9;
            agariTable[0x20200006d8] = 0xf9;
            agariTable[0x2000013600] = 0xa9;
            agariTable[0x1b000800] = 0xa9;
            agariTable[0x610] = 0x57;
            agariTable[0x8006c0] = 0xa9;
            agariTable[0x4126d8000] = 0xf9;
            agariTable[0x1b612010] = 0xf9;
            agariTable[0x6c0410000] = 0xa9;
            agariTable[0x2480] = 0x65;
            agariTable[0x83602000] = 0xa9;
            agariTable[0x83680000] = 0xa9;
            agariTable[0x20001006d8] = 0xf9;
            agariTable[0x4db000480] = 0xf9;
            agariTable[0xa06d8000] = 0xf9;
            agariTable[0x26c0400] = 0xa9;
            agariTable[0x3600480] = 0xa9;
            agariTable[0x201b080000] = 0xa9;
            agariTable[0x3600000100] = 0xa9;
            agariTable[0x24800db000] = 0xf9;
            agariTable[0x201001b680] = 0xf9;
            agariTable[0x6e0000] = 0xa9;
            agariTable[0x6c0080400] = 0xa9;
            agariTable[0x3600002010] = 0xa9;
            agariTable[0x100c0] = 0x57;
            agariTable[0x400000018] = 0x57;
            agariTable[0xc0400] = 0x57;
            agariTable[0x100136d0] = 0xf9;
            agariTable[0x8020db000] = 0xf9;
            agariTable[0x26d8400080] = 0xf9;
            agariTable[0x3680000010] = 0xa9;
            agariTable[0x2492400000] = 0xc5;
            agariTable[0x806da080] = 0xf9;
            agariTable[0x20800106d8] = 0xf9;
            agariTable[0x1b800] = 0xa9;
            agariTable[0x92000000] = 0x65;
            agariTable[0x18000010] = 0x57;
            agariTable[0x100206d8] = 0xf9;
            agariTable[0x836d0400] = 0xf9;
            agariTable[0x492480000] = 0xc5;
            agariTable[0x80600] = 0x57;
            agariTable[0x200106d8] = 0xf9;
            agariTable[0x24000806d8] = 0xf9;
            agariTable[0x10403600] = 0xa9;
            agariTable[0x4036c0090] = 0xf9;
            agariTable[0x201b680400] = 0xf9;
            agariTable[0x36d0400010] = 0xf9;
            agariTable[0x2480000] = 0x65;
            agariTable[0x1041b680] = 0xf9;
            agariTable[0x8036c0010] = 0xf9;
            agariTable[0x20136d0000] = 0xf9;
            agariTable[0x6c0004000] = 0xa9;
            agariTable[0x36c0412000] = 0xf9;
            agariTable[0x3000080000] = 0x57;
            agariTable[0x800db020] = 0xf9;
            agariTable[0x3602010000] = 0xa9;
            agariTable[0x80600000] = 0x57;
            agariTable[0x20003600] = 0xa9;
            agariTable[0x106c2000] = 0xa9;
            agariTable[0x6d8080020] = 0xf9;
            agariTable[0x2600] = 0x57;
            agariTable[0x920036c0] = 0xf9;
            agariTable[0x20000806c0] = 0xa9;
            agariTable[0x8800036c0] = 0xf9;
            agariTable[0x26da400] = 0xf9;
            agariTable[0x6c0100] = 0xa9;
            agariTable[0x6da402000] = 0xf9;
            agariTable[0x12400] = 0x65;
            agariTable[0x846d8] = 0xf9;
            agariTable[0x20004136c0] = 0xf9;
            agariTable[0x24836c0] = 0xf9;
            agariTable[0x40209b600] = 0xf9;
            agariTable[0x36c0880] = 0xf9;
            agariTable[0x20036e0000] = 0xf9;
            agariTable[0x400600] = 0x57;
            agariTable[0x20100006c0] = 0xa9;
            agariTable[0x402003600] = 0xa9;
            agariTable[0x6d8080100] = 0xf9;
            agariTable[0x36c0020080] = 0xf9;
            agariTable[0x6d8] = 0x98;
            agariTable[0x492480] = 0xc5;
            agariTable[0x28000006d8] = 0xf9;
            agariTable[0x3600020] = 0xa9;
            agariTable[0x1b700080] = 0xf9;
            agariTable[0x36d0020000] = 0xf9;
            agariTable[0x2000000000] = 0x24;
            agariTable[0x1049b600] = 0xf9;
            agariTable[0x6da400400] = 0xf9;
            agariTable[0x10003680] = 0xa9;
            agariTable[0x81b680] = 0xf9;
            agariTable[0x41b400] = 0xa9;
            agariTable[0x104db080] = 0xf9;
            agariTable[0x804db080] = 0xf9;
            agariTable[0x136d0080] = 0xf9;
            agariTable[0x8136c0000] = 0xf9;
            agariTable[0x41b604000] = 0xf9;
            agariTable[0x803600] = 0xa9;
            agariTable[0x51b600000] = 0xf9;
            agariTable[0x49b680000] = 0xf9;
            agariTable[0x801036c0] = 0xf9;
            agariTable[0x200009b000] = 0xa9;
            agariTable[0x836d2000] = 0xf9;
            agariTable[0xd8480000] = 0xa9;
            agariTable[0x6da000020] = 0xf9;
            agariTable[0x800000000] = 0x42;
            agariTable[0x20020000d8] = 0xa9;
            agariTable[0x1001b690] = 0xf9;
            agariTable[0x21006d8000] = 0xf9;
            agariTable[0x1036d0] = 0xf9;
            agariTable[0x82003600] = 0xa9;
            agariTable[0x240001b610] = 0xf9;
            agariTable[0xdb0a0] = 0xf9;
            agariTable[0x20906d8000] = 0xf9;
            agariTable[0xda010000] = 0xa9;
            agariTable[0x36c0000500] = 0xf9;
            agariTable[0x4036e0] = 0xf9;
            agariTable[0x41009b600] = 0xf9;
            agariTable[0xdb010800] = 0xf9;
            agariTable[0x4d8002000] = 0xa9;
            agariTable[0x6da010010] = 0xf9;
            agariTable[0x6c2002000] = 0xa9;
            agariTable[0x4003600] = 0xa9;
            agariTable[0x206d8010] = 0xf9;
            agariTable[0x126d8080] = 0xf9;
            agariTable[0x6d8000500] = 0xf9;
            agariTable[0x1b100] = 0xa9;
            agariTable[0x4d8000080] = 0xa9;
            agariTable[0x1b6d8] = 0xf8;
            agariTable[0x4020d8] = 0xa9;
            agariTable[0x1401b600] = 0xf9;
            agariTable[0x6da800] = 0xf9;
            agariTable[0x6d8002100] = 0xf9;
            agariTable[0x36d8000000] = 0xc8;
            agariTable[0x20024006d8] = 0xf9;
            agariTable[0x808036c0] = 0xf9;
            agariTable[0x20100db080] = 0xf9;
            agariTable[0x3602080] = 0xa9;
            agariTable[0x1b682010] = 0xf9;
            agariTable[0x26d8800000] = 0xf9;
            agariTable[0x36c00a0000] = 0xf9;
            agariTable[0xd0000000] = 0x57;
            agariTable[0x804836c0] = 0xf9;
            agariTable[0x4026c0000] = 0xa9;
            agariTable[0x36d0410000] = 0xf9;
            agariTable[0x249249248] = 0xb1;
            agariTable[0x20020db080] = 0xf9;
            agariTable[0x36c4400] = 0xf9;
            agariTable[0x36d0080400] = 0xf9;
            agariTable[0x200036d0] = 0xf9;
            agariTable[0x146d8] = 0xf9;
            agariTable[0x21006d8] = 0xf9;
            agariTable[0x248001b600] = 0xf9;
            agariTable[0x200db400] = 0xf9;
            agariTable[0x20006d8800] = 0xf9;
            agariTable[0xdb002100] = 0xf9;
            agariTable[0x20d8000080] = 0xa9;
            agariTable[0xc0400000] = 0x57;
            agariTable[0x36c2000800] = 0xf9;
            agariTable[0x11001b600] = 0xf9;
            agariTable[0x4106d8080] = 0xf9;
            agariTable[0x20036d0010] = 0xf9;
            agariTable[0x49b602000] = 0xf9;
            agariTable[0x81b610000] = 0xf9;
            agariTable[0x281b600000] = 0xf9;
            agariTable[0x20004036d0] = 0xf9;
            agariTable[0x1b602800] = 0xf9;
            agariTable[0x3600480000] = 0xa9;
            agariTable[0x1b620010] = 0xf9;
            agariTable[0x240001b680] = 0xf9;
            agariTable[0x1b020000] = 0xa9;
            agariTable[0x4db082000] = 0xf9;
            agariTable[0x36c0100400] = 0xf9;
            agariTable[0x2080003600] = 0xa9;
            agariTable[0x600400] = 0x57;
            agariTable[0x800036e0] = 0xf9;
            agariTable[0x200836c0] = 0xf9;
            agariTable[0x11b680] = 0xf9;
            agariTable[0x24000db080] = 0xf9;
            agariTable[0x800d8010] = 0xa9;
            agariTable[0x24006d8400] = 0xf9;
            agariTable[0x12492000] = 0xc5;
            agariTable[0x20db000410] = 0xf9;
            agariTable[0x36c0010020] = 0xf9;
            agariTable[0x40026d8] = 0xf9;
            agariTable[0x4036e0000] = 0xf9;
            agariTable[0x201b600410] = 0xf9;
            agariTable[0x1b690400] = 0xf9;
            agariTable[0x36d0000020] = 0xf9;
            agariTable[0x4006dc000] = 0xf9;
            agariTable[0x106dc000] = 0xf9;
            agariTable[0x4c0] = 0x57;
            agariTable[0x20800d8] = 0xa9;
            agariTable[0x40009b610] = 0xf9;
            agariTable[0x241b680] = 0xf9;
            agariTable[0x20000d8010] = 0xa9;
            agariTable[0x20006d8090] = 0xf9;
            agariTable[0x3000] = 0x36;
            agariTable[0x3000000400] = 0x57;
            agariTable[0x4100026d8] = 0xf9;
            agariTable[0x10041b600] = 0xf9;
            agariTable[0xdb022000] = 0xf9;
            agariTable[0xdb6c0000] = 0xf8;
            agariTable[0x4004006c0] = 0xa9;
            agariTable[0x820db080] = 0xf9;
            agariTable[0x6d8110] = 0xf9;
            agariTable[0x1026d8000] = 0xf9;
            agariTable[0x20136c0400] = 0xf9;
            agariTable[0x1b012000] = 0xa9;
            agariTable[0x24db010000] = 0xf9;
            agariTable[0xdb000] = 0x98;
            agariTable[0x36c0100010] = 0xf9;
            agariTable[0xd8080400] = 0xa9;
            agariTable[0xdb802000] = 0xf9;
            agariTable[0x4206d8] = 0xf9;
            agariTable[0x104136c0] = 0xf9;
            agariTable[0x20004db010] = 0xf9;
            agariTable[0x46da000] = 0xf9;
            agariTable[0x4d8400000] = 0xa9;
            agariTable[0x100000c0] = 0x57;
            agariTable[0x9001b000] = 0xa9;
            agariTable[0x36c2002400] = 0xf9;
            agariTable[0x3690000000] = 0xa9;
            agariTable[0x1006c0] = 0xa9;
            agariTable[0x806c0400] = 0xa9;
            agariTable[0x36c2000410] = 0xf9;
            agariTable[0x40041b000] = 0xa9;
            agariTable[0x600010] = 0x57;
            agariTable[0x80001b680] = 0xf9;
            agariTable[0x1b090000] = 0xa9;
            agariTable[0xda080000] = 0xa9;
            agariTable[0x36d0100000] = 0xf9;
            agariTable[0xd0000] = 0x57;
            agariTable[0x20106da000] = 0xf9;
            agariTable[0xdb100010] = 0xf9;
            agariTable[0x4000d8080] = 0xa9;
            agariTable[0x9b700000] = 0xf9;
            agariTable[0x20db080010] = 0xf9;
            agariTable[0xdb600000] = 0xc8;
            agariTable[0x26c0080] = 0xa9;
            agariTable[0x81b602000] = 0xf9;
            agariTable[0x20db002400] = 0xf9;
            agariTable[0xda400000] = 0xa9;
            agariTable[0x36c0400800] = 0xf9;
            agariTable[0x20800026d8] = 0xf9;
            agariTable[0x1b600490] = 0xf9;
            agariTable[0x1201b680] = 0xf9;
            agariTable[0x208001b000] = 0xa9;
            agariTable[0x4db000020] = 0xf9;
            agariTable[0x3400] = 0x57;
            agariTable[0x200081b600] = 0xf9;
            agariTable[0x46d8010] = 0xf9;
            agariTable[0x4036d2000] = 0xf9;
            agariTable[0x3600080010] = 0xa9;
            agariTable[0x6db000000] = 0xc8;
            agariTable[0x826d8010] = 0xf9;
            agariTable[0x3680010] = 0xa9;
            agariTable[0x1b602020] = 0xf9;
            agariTable[0x41b010000] = 0xa9;
            agariTable[0xd8000800] = 0xa9;
            agariTable[0x6d8110000] = 0xf9;
            agariTable[0x36c0010800] = 0xf9;
            agariTable[0x820026d8] = 0xf9;
            agariTable[0x3690] = 0xa9;
            agariTable[0x3680000080] = 0xa9;
            agariTable[0x824006d8] = 0xf9;
            agariTable[0x124036c0] = 0xf9;
            agariTable[0x8006d8400] = 0xf9;
            agariTable[0xdb482000] = 0xf9;
            agariTable[0x6c2400000] = 0xa9;
            agariTable[0x36d0080010] = 0xf9;
            agariTable[0x8d8] = 0xa9;
            agariTable[0x20826d8] = 0xf9;
            agariTable[0x8001b400] = 0xa9;
            agariTable[0x1201b000] = 0xa9;
            agariTable[0xda400] = 0xa9;
            agariTable[0x1b010400] = 0xa9;
            agariTable[0x90] = 0x45;
            agariTable[0x80003610] = 0xa9;
            agariTable[0xdb014000] = 0xf9;
            agariTable[0x6d8000810] = 0xf9;
            agariTable[0x20104036c0] = 0xf9;
            agariTable[0x20806d8080] = 0xf9;
            agariTable[0xdb000110] = 0xf9;
            agariTable[0x20020026d8] = 0xf9;
            agariTable[0x20db000090] = 0xf9;
            agariTable[0x36c0082080] = 0xf9;
            agariTable[0x41b080] = 0xa9;
            agariTable[0x800004d8] = 0xa9;
            agariTable[0x9b620000] = 0xf9;
            agariTable[0x41b000400] = 0xa9;
            agariTable[0x6da080080] = 0xf9;
            agariTable[0x3602080000] = 0xa9;
            agariTable[0x600] = 0x36;
            agariTable[0x201b000400] = 0xa9;
            agariTable[0x36d2000080] = 0xf9;
            agariTable[0x5006d8000] = 0xf9;
            agariTable[0x824db000] = 0xf9;
            agariTable[0x36d0100] = 0xf9;
            agariTable[0x104000d8] = 0xa9;
            agariTable[0x8026d8] = 0xf9;
            agariTable[0x20800006c0] = 0xa9;
            agariTable[0x904036c0] = 0xf9;
            agariTable[0x4100db080] = 0xf9;
            agariTable[0x20d8000010] = 0xa9;
            agariTable[0x490000000] = 0x65;
            agariTable[0x41b000010] = 0xa9;
            agariTable[0x11b610] = 0xf9;
            agariTable[0x4db090] = 0xf9;
            agariTable[0x108db000] = 0xf9;
            agariTable[0x6dc400000] = 0xf9;
            agariTable[0x36c0080480] = 0xf9;
            agariTable[0x211b600] = 0xf9;
            agariTable[0x1b680100] = 0xf9;
            agariTable[0x6da002400] = 0xf9;
            agariTable[0x3602400000] = 0xa9;
            agariTable[0x20db090] = 0xf9;
            agariTable[0x83000] = 0x57;
            agariTable[0x20020136c0] = 0xf9;
            agariTable[0x26d8020] = 0xf9;
            agariTable[0x1b612400] = 0xf9;
            agariTable[0x6d8400410] = 0xf9;
            agariTable[0x26d8000480] = 0xf9;
            agariTable[0x90000] = 0x45;
            agariTable[0x41001b000] = 0xa9;
            agariTable[0x8100db000] = 0xf9;
            agariTable[0x1000d8000] = 0xa9;
            agariTable[0x100806c0] = 0xa9;
            agariTable[0x20004db080] = 0xf9;
            agariTable[0x8006d8010] = 0xf9;
            agariTable[0x413600000] = 0xa9;
            agariTable[0x4db002010] = 0xf9;
            agariTable[0x4db080080] = 0xf9;
            agariTable[0x12003600] = 0xa9;
            agariTable[0x100036e0] = 0xf9;
            agariTable[0x804136c0] = 0xf9;
            agariTable[0x201041b600] = 0xf9;
            agariTable[0xdb090010] = 0xf9;
            agariTable[0x906c0] = 0xa9;
            agariTable[0x4100836c0] = 0xf9;
            agariTable[0x124db000] = 0xf9;
            agariTable[0x24136c0000] = 0xf9;
            agariTable[0xdb000420] = 0xf9;
            agariTable[0x200136c0] = 0xf9;
            agariTable[0x92490] = 0xc5;
            agariTable[0x12490000] = 0xa5;
            agariTable[0x20100026d8] = 0xf9;
            agariTable[0x1020006d8] = 0xf9;
            agariTable[0x800236c0] = 0xf9;
            agariTable[0x836c2080] = 0xf9;
            agariTable[0x26da000400] = 0xf9;
            agariTable[0x1000000] = 0x13;
            agariTable[0x36d0000480] = 0xf9;
            agariTable[0x100db090] = 0xf9;
            agariTable[0x1b000020] = 0xa9;
            agariTable[0x492490] = 0xe5;
            agariTable[0xdb500000] = 0xf9;
            agariTable[0x20036c0800] = 0xf9;
            agariTable[0x20db480] = 0xf9;
            agariTable[0x1b684000] = 0xf9;
            agariTable[0x36c0000810] = 0xf9;
            agariTable[0x36c2080080] = 0xf9;
            agariTable[0x10018] = 0x57;
            agariTable[0x4100036d0] = 0xf9;
            agariTable[0x8106d8000] = 0xf9;
            agariTable[0x4036c0480] = 0xf9;
            agariTable[0x92000] = 0x65;
            agariTable[0xdb412000] = 0xf9;
            agariTable[0xc0000080] = 0x57;
            agariTable[0x20136c0080] = 0xf9;
            agariTable[0x3620000] = 0xa9;
            agariTable[0x41b680010] = 0xf9;
            agariTable[0x6d8402010] = 0xf9;
            agariTable[0x36d0010400] = 0xf9;
            agariTable[0x36c2410000] = 0xf9;
            agariTable[0x104db010] = 0xf9;
            agariTable[0x6e0] = 0xa9;
            agariTable[0x8049b600] = 0xf9;
            agariTable[0x241001b600] = 0xf9;
            agariTable[0x804db010] = 0xf9;
            agariTable[0x20806da000] = 0xf9;
            agariTable[0x1136c0000] = 0xf9;
            agariTable[0x800020d8] = 0xa9;
            agariTable[0x4126d8] = 0xf9;
            agariTable[0x40241b600] = 0xf9;
            agariTable[0xdb004400] = 0xf9;
            agariTable[0x492490000] = 0xe5;
            agariTable[0x41041b600] = 0xf9;
            agariTable[0x6d0400000] = 0xa9;
            agariTable[0x2013600] = 0xa9;
            agariTable[0x20800836c0] = 0xf9;
            agariTable[0x80001b610] = 0xf9;
            agariTable[0x4006c0400] = 0xa9;
            agariTable[0x201b602010] = 0xf9;
            agariTable[0x4000026c0] = 0xa9;
            agariTable[0x9b690] = 0xf9;
            agariTable[0x4000d8010] = 0xa9;
            agariTable[0x806da010] = 0xf9;
            agariTable[0x241b600010] = 0xf9;
            agariTable[0xdb402010] = 0xf9;
            agariTable[0xdb400480] = 0xf9;
            agariTable[0x201b620] = 0xf9;
            agariTable[0x20804db000] = 0xf9;
            agariTable[0x40001b010] = 0xa9;
            agariTable[0x20000046d8] = 0xf9;
            agariTable[0x40081b600] = 0xf9;
            agariTable[0x1b680800] = 0xf9;
            agariTable[0x26d8000410] = 0xf9;
            agariTable[0xc0000400] = 0x57;
            agariTable[0x200011b600] = 0xf9;
            agariTable[0x6d8400800] = 0xf9;
            agariTable[0x3600100000] = 0xa9;
            agariTable[0x492000] = 0x85;
            agariTable[0x8136c0] = 0xf9;
            agariTable[0x400003680] = 0xa9;
            agariTable[0x80403600] = 0xa9;
            agariTable[0x804db400] = 0xf9;
            agariTable[0x36c2020] = 0xf9;
            agariTable[0xdb400410] = 0xf9;
            agariTable[0x6da410000] = 0xf9;
            agariTable[0x92490000] = 0xc5;
            agariTable[0x6c2080] = 0xa9;
            agariTable[0x20d8010000] = 0xa9;
            agariTable[0x20000d8400] = 0xa9;
            agariTable[0x8026d8000] = 0xf9;
            agariTable[0x1006c0000] = 0xa9;
            agariTable[0x26d8010080] = 0xf9;
            agariTable[0x36c0002020] = 0xf9;
            agariTable[0x44006d8] = 0xf9;
            agariTable[0x240001b000] = 0xa9;
            agariTable[0x4806d8400] = 0xf9;
            agariTable[0x36c0404000] = 0xf9;
            agariTable[0x3600080400] = 0xa9;
            agariTable[0x840006d8] = 0xf9;
            agariTable[0x492000000] = 0x85;
            agariTable[0x21000036c0] = 0xf9;
            agariTable[0x13610000] = 0xa9;
            agariTable[0xdb012010] = 0xf9;
            agariTable[0x12492480] = 0x105;
            agariTable[0x2000600000] = 0x57;
            agariTable[0x900d8] = 0xa9;
            agariTable[0x204006d8] = 0xf9;
            agariTable[0x820db010] = 0xf9;
            agariTable[0x36c2410] = 0xf9;
            agariTable[0x9b610080] = 0xf9;
            agariTable[0xdb810000] = 0xf9;
            agariTable[0x3080] = 0x57;
            agariTable[0x8009b680] = 0xf9;
            agariTable[0x800db090] = 0xf9;
            agariTable[0x8db010] = 0xf9;
            agariTable[0x20036c0090] = 0xf9;
            agariTable[0x5036c0000] = 0xf9;
            agariTable[0x3680400] = 0xa9;
            agariTable[0x9b600100] = 0xf9;
            agariTable[0xd8000000] = 0x68;
            agariTable[0x24000db010] = 0xf9;
            agariTable[0x8800db000] = 0xf9;
            agariTable[0x1026d8] = 0xf9;
            agariTable[0x824036c0] = 0xf9;
            agariTable[0xd8010400] = 0xa9;
            agariTable[0x4020026d8] = 0xf9;
            agariTable[0x20c0] = 0x57;
            agariTable[0x4db100000] = 0xf9;
            agariTable[0x3600] = 0x68;
            agariTable[0x201b010] = 0xa9;
            agariTable[0x20000100d8] = 0xa9;
            agariTable[0x1b000000] = 0x68;
            agariTable[0x20001036c0] = 0xf9;
            agariTable[0x100003600] = 0xa9;
            agariTable[0x6c2400] = 0xa9;
            agariTable[0xd8100000] = 0xa9;
            agariTable[0x6d8010800] = 0xf9;
            agariTable[0x6d8800400] = 0xf9;
            agariTable[0x8000] = 0x13;
            agariTable[0x201b604000] = 0xf9;
            agariTable[0x1b480] = 0xa9;
            agariTable[0x6d8000490] = 0xf9;
            agariTable[0x6da000480] = 0xf9;
            agariTable[0x920006d8] = 0xf9;
            agariTable[0xc0080000] = 0x57;
            agariTable[0x100026c0] = 0xa9;
            agariTable[0x100936c0] = 0xf9;
            agariTable[0x4006d0000] = 0xa9;
            agariTable[0x36c0004080] = 0xf9;
            agariTable[0x1249200] = 0x61;
            agariTable[0x8db000010] = 0xf9;
            agariTable[0x6d8002090] = 0xf9;
            agariTable[0x40836c0] = 0xf9;
            agariTable[0x4836d0] = 0xf9;
            agariTable[0x4024db000] = 0xf9;
            agariTable[0x20106d8400] = 0xf9;
            agariTable[0x8db000080] = 0xf9;
            agariTable[0x6d8400020] = 0xf9;
            agariTable[0x1000106d8] = 0xf9;
            agariTable[0x36c0410080] = 0xf9;
            agariTable[0x20000000c0] = 0x57;
            agariTable[0x120db400] = 0xf9;
            agariTable[0x20024db000] = 0xf9;
            agariTable[0xdb080090] = 0xf9;
            agariTable[0x6c0402000] = 0xa9;
            agariTable[0x4800026d8] = 0xf9;
            agariTable[0x209001b600] = 0xf9;
            agariTable[0x26c0010] = 0xa9;
            agariTable[0x36c2400080] = 0xf9;
            agariTable[0x12000] = 0x45;
            agariTable[0x80003680] = 0xa9;
            agariTable[0x2041b600] = 0xf9;
            agariTable[0x120d8000] = 0xa9;
            agariTable[0x201b610080] = 0xf9;
            agariTable[0x6d8480400] = 0xf9;
            agariTable[0x2003000000] = 0x57;
            agariTable[0x1b682400] = 0xf9;
            agariTable[0x480000000] = 0x45;
            agariTable[0x21000db000] = 0xf9;
            agariTable[0x24006d8010] = 0xf9;
            agariTable[0x410003600] = 0xa9;
            agariTable[0x26d8090] = 0xf9;
            agariTable[0x20006d8410] = 0xf9;
            agariTable[0x136c2080] = 0xf9;
            agariTable[0x3680080] = 0xa9;
            agariTable[0x4004000d8] = 0xa9;
            agariTable[0x101036c0] = 0xf9;
            agariTable[0x46d8080] = 0xf9;
            agariTable[0x41b690000] = 0xf9;
            agariTable[0x4db402000] = 0xf9;
            agariTable[0x12492400] = 0xe5;
            agariTable[0x2000000600] = 0x57;
            agariTable[0x24db400] = 0xf9;
            agariTable[0x4db400010] = 0xf9;
            agariTable[0x3680000400] = 0xa9;
            agariTable[0x2492000000] = 0xa5;
            agariTable[0x4000936c0] = 0xf9;
            agariTable[0x24000d8000] = 0xa9;
            agariTable[0x36c2004000] = 0xf9;
            agariTable[0x2490000000] = 0x85;
            agariTable[0x3600010080] = 0xa9;
            agariTable[0x804d8000] = 0xa9;
            agariTable[0x20200036c0] = 0xf9;
            agariTable[0x20120db000] = 0xf9;
            agariTable[0x36c0110] = 0xf9;
            agariTable[0x241b600080] = 0xf9;
            agariTable[0x26da000010] = 0xf9;
            agariTable[0x610000000] = 0x57;
            agariTable[0x6d8014000] = 0xf9;
            agariTable[0x40201b000] = 0xa9;
            agariTable[0x3600090] = 0xa9;
            agariTable[0x8db010000] = 0xf9;
            agariTable[0x9b700] = 0xf9;
            agariTable[0x140036c0] = 0xf9;
            agariTable[0x8201b610] = 0xf9;
            agariTable[0x6d8012010] = 0xf9;
            agariTable[0x4000] = 0x42;
            agariTable[0x10] = 0x24;
            agariTable[0x10009b600] = 0xf9;
            agariTable[0x11b602000] = 0xf9;
            agariTable[0x6d8400480] = 0xf9;
            agariTable[0x200041b610] = 0xf9;
            agariTable[0x1036c0010] = 0xf9;
            agariTable[0x20db090000] = 0xf9;
            agariTable[0xd8000410] = 0xa9;
            agariTable[0x200db080] = 0xf9;
            agariTable[0x1b100000] = 0xa9;
            agariTable[0x1249240] = 0x71;
            agariTable[0x20806c0] = 0xa9;
            agariTable[0xdb082010] = 0xf9;
            agariTable[0x6d8100010] = 0xf9;
            agariTable[0x2400] = 0x45;
            agariTable[0x3600004000] = 0xa9;
            agariTable[0x490000] = 0x65;
            agariTable[0x2018] = 0x57;
            agariTable[0x400018000] = 0x57;
            agariTable[0x8d8000] = 0xa9;
            agariTable[0x104d8000] = 0xa9;
            agariTable[0x6d8500000] = 0xf9;
            agariTable[0x36d2010000] = 0xf9;
            agariTable[0x249200] = 0x51;
            agariTable[0x6c0082000] = 0xa9;
            agariTable[0x40049b600] = 0xf9;
            agariTable[0x403680] = 0xa9;
            agariTable[0x820d8000] = 0xa9;
            agariTable[0x36c0020400] = 0xf9;
            agariTable[0x2490] = 0x85;
            agariTable[0x9249200] = 0x71;
            agariTable[0x3400000] = 0x57;
            agariTable[0x4100006c0] = 0xa9;
            agariTable[0x20236c0] = 0xf9;
            agariTable[0x20100136c0] = 0xf9;
            agariTable[0x2003600080] = 0xa9;
            agariTable[0x8] = 0x13;
            agariTable[0x4800d8000] = 0xa9;
            agariTable[0x4036c2080] = 0xf9;
            agariTable[0x6d8090080] = 0xf9;
            agariTable[0x6c4000000] = 0xa9;
            agariTable[0x36e2000000] = 0xf9;
            agariTable[0x5006d8] = 0xf9;
            agariTable[0x240009b600] = 0xf9;
            agariTable[0x201b400] = 0xa9;
            agariTable[0x4040036c0] = 0xf9;
            agariTable[0x204036c0] = 0xf9;
            agariTable[0x104db400] = 0xf9;
            agariTable[0x20000020d8] = 0xa9;
            agariTable[0x24836c0000] = 0xf9;
            agariTable[0x9b682000] = 0xf9;
            agariTable[0xdb420000] = 0xf9;
            agariTable[0x6da020000] = 0xf9;
            agariTable[0x24006c0000] = 0xa9;
            agariTable[0x20104006d8] = 0xf9;
            agariTable[0x120836c0] = 0xf9;
            agariTable[0x208009b600] = 0xf9;
            agariTable[0x108006d8] = 0xf9;
            agariTable[0xc0010] = 0x57;
            agariTable[0x42001b600] = 0xf9;
            agariTable[0x20126d8000] = 0xf9;
            agariTable[0x24036c0010] = 0xf9;
            agariTable[0x136c0100] = 0xf9;
            agariTable[0x36c0810000] = 0xf9;
            agariTable[0x2492000] = 0xa5;
            agariTable[0x20db002080] = 0xf9;
            agariTable[0x8004036c0] = 0xf9;
            agariTable[0x18010] = 0x57;
            agariTable[0x480003600] = 0xa9;
            agariTable[0x906da000] = 0xf9;
            agariTable[0x8d8000000] = 0xa9;
            agariTable[0x6d8400100] = 0xf9;
            agariTable[0x2492490] = 0x105;
            agariTable[0x1000006c0] = 0xa9;
            agariTable[0xa00036c0] = 0xf9;
            agariTable[0x10001b680] = 0xf9;
            agariTable[0x3602400] = 0xa9;
            agariTable[0x1b000090] = 0xa9;
            agariTable[0xdb080800] = 0xf9;
            agariTable[0x6d8004010] = 0xf9;
            agariTable[0x4020000d8] = 0xa9;
            agariTable[0x24136c0] = 0xf9;
            agariTable[0x4db020] = 0xf9;
            agariTable[0x20000db100] = 0xf9;
            agariTable[0x20036c2010] = 0xf9;
            agariTable[0x236c2000] = 0xf9;
            agariTable[0x83600080] = 0xa9;
            agariTable[0x41b680080] = 0xf9;
            agariTable[0x24000d8] = 0xa9;
            agariTable[0x4106d8400] = 0xf9;
            agariTable[0x201b600800] = 0xf9;
            agariTable[0x1b692000] = 0xf9;
            agariTable[0xd8000480] = 0xa9;
            agariTable[0x4826d8] = 0xf9;
            agariTable[0x900026d8] = 0xf9;
            agariTable[0x4024006d8] = 0xf9;
            agariTable[0x200001b620] = 0xf9;
            agariTable[0x200001b080] = 0xa9;
            agariTable[0x36c0402010] = 0xf9;
            agariTable[0x2000000] = 0x24;
            agariTable[0x8000db010] = 0xf9;
            agariTable[0x106d8410] = 0xf9;
            agariTable[0xdb004010] = 0xf9;
            agariTable[0x4c0000] = 0x57;
            agariTable[0x4806c0] = 0xa9;
            agariTable[0x36e0400000] = 0xf9;
            agariTable[0xd0] = 0x57;
            agariTable[0x100236c0] = 0xf9;
            agariTable[0x2400003600] = 0xa9;
            agariTable[0x202001b600] = 0xf9;
            agariTable[0x2003600010] = 0xa9;
            agariTable[0x83610000] = 0xa9;
            agariTable[0x36d0080080] = 0xf9;
            agariTable[0x20008006d8] = 0xf9;
            agariTable[0x100c0000] = 0x57;
            agariTable[0x20046d8] = 0xf9;
            agariTable[0x4100136c0] = 0xf9;
            agariTable[0x200001b690] = 0xf9;
            agariTable[0x281b600] = 0xf9;
            agariTable[0x100d8080] = 0xa9;
            agariTable[0x2003600400] = 0xa9;
            agariTable[0x80000018] = 0x57;
            agariTable[0x6d8090010] = 0xf9;
            agariTable[0xd8000090] = 0xa9;
            agariTable[0x100006d0] = 0xa9;
            agariTable[0xdb500] = 0xf9;
            agariTable[0x6d0000400] = 0xa9;
            agariTable[0x36c2800000] = 0xf9;
            agariTable[0x3600002080] = 0xa9;
            agariTable[0x249248000] = 0x71;
            agariTable[0x36d0002400] = 0xf9;
            agariTable[0x20000936c0] = 0xf9;
            agariTable[0x18002000] = 0x57;
            agariTable[0x900136c0] = 0xf9;
            agariTable[0x2001b610] = 0xf9;
            agariTable[0x20000db090] = 0xf9;
            agariTable[0x226d8000] = 0xf9;
            agariTable[0x602000] = 0x57;
            agariTable[0x4d8010] = 0xa9;
            agariTable[0x4000db800] = 0xf9;
            agariTable[0x100db800] = 0xf9;
            agariTable[0x8db400000] = 0xf9;
            agariTable[0x26d8002080] = 0xf9;
            agariTable[0x26d8080400] = 0xf9;
            agariTable[0x3080000000] = 0x57;
            agariTable[0x836c0020] = 0xf9;
            agariTable[0x136c0480] = 0xf9;
            agariTable[0x13680000] = 0xa9;
            agariTable[0x36d0010010] = 0xf9;
            agariTable[0x36c0880000] = 0xf9;
            agariTable[0x1b710] = 0xf9;
            agariTable[0xd8] = 0x68;
            agariTable[0xdb6c0] = 0xf8;
            agariTable[0x4806d8080] = 0xf9;
            agariTable[0x6d8100080] = 0xf9;
            agariTable[0x2492400] = 0xc5;
            agariTable[0x400083600] = 0xa9;
            agariTable[0x600010000] = 0x57;
            agariTable[0x800906d8] = 0xf9;
            agariTable[0x4004836c0] = 0xf9;
            agariTable[0x49248000] = 0x61;
            agariTable[0x4200db000] = 0xf9;
            agariTable[0x4036c4000] = 0xf9;
            agariTable[0x2403600000] = 0xa9;
            agariTable[0x9001b680] = 0xf9;
            agariTable[0x1006d8010] = 0xf9;
            agariTable[0x836c2010] = 0xf9;
            agariTable[0x936c0080] = 0xf9;
            agariTable[0x201b620000] = 0xf9;
            agariTable[0x3600010400] = 0xa9;
            agariTable[0x201201b600] = 0xf9;
            agariTable[0x1b020] = 0xa9;
            agariTable[0x401b000] = 0xa9;
            agariTable[0x100db100] = 0xf9;
            agariTable[0x8006d8080] = 0xf9;
            agariTable[0x20836c0010] = 0xf9;
            agariTable[0x1b010080] = 0xa9;
            agariTable[0x6c0002400] = 0xa9;
            agariTable[0x4800136c0] = 0xf9;
            agariTable[0x3600080080] = 0xa9;
            agariTable[0x418000000] = 0x57;
            agariTable[0x20000126d8] = 0xf9;
            agariTable[0xa00006d8] = 0xf9;
            agariTable[0x20db020] = 0xf9;
            agariTable[0x6d8002800] = 0xf9;
            agariTable[0x36c0014000] = 0xf9;
            agariTable[0x49249000] = 0x71;
            agariTable[0x36c0000110] = 0xf9;
            agariTable[0x826da000] = 0xf9;
            agariTable[0x3400000000] = 0x57;
            agariTable[0x8000000] = 0x13;
            agariTable[0x2000018000] = 0x57;
            agariTable[0x3002000] = 0x57;
            agariTable[0x20100806d8] = 0xf9;
            agariTable[0x20db400400] = 0xf9;
            agariTable[0x2490000] = 0x85;
            agariTable[0x6dc000010] = 0xf9;
            agariTable[0x2000000018] = 0x57;
            agariTable[0x98000] = 0x57;
            agariTable[0x24026d8] = 0xf9;
            agariTable[0x3610080] = 0xa9;
            agariTable[0x1b690080] = 0xf9;
            agariTable[0x36d8000] = 0xc8;
            agariTable[0x13680] = 0xa9;
            agariTable[0x4020db400] = 0xf9;
            agariTable[0x4804db000] = 0xf9;
            agariTable[0x9b600480] = 0xf9;
            agariTable[0x36c2082000] = 0xf9;
            agariTable[0x80000] = 0x24;
            agariTable[0x400600000] = 0x57;
            agariTable[0x8004db000] = 0xf9;
            agariTable[0x93600000] = 0xa9;
            agariTable[0xd8020000] = 0xa9;
            agariTable[0x26da000080] = 0xf9;
            agariTable[0x26c0010000] = 0xa9;
            agariTable[0x4000000c0] = 0x57;
            agariTable[0x100106c0] = 0xa9;
            agariTable[0x900d8000] = 0xa9;
            agariTable[0x6c0020] = 0xa9;
            agariTable[0x3600800] = 0xa9;
            agariTable[0x36c0480400] = 0xf9;
            agariTable[0x36c0802000] = 0xf9;
            agariTable[0x98000000] = 0x57;
            agariTable[0xd8090] = 0xa9;
            agariTable[0x4026d8400] = 0xf9;
            agariTable[0x926d8000] = 0xf9;
            agariTable[0xdb480010] = 0xf9;
            agariTable[0x209b680] = 0xf9;
            agariTable[0x40db080] = 0xf9;
            agariTable[0x20036d0400] = 0xf9;
            agariTable[0x9b400000] = 0xa9;
            agariTable[0xdb002800] = 0xf9;
            agariTable[0x9248000] = 0x51;
            agariTable[0x680000] = 0x57;
            agariTable[0x100da000] = 0xa9;
            agariTable[0x136c0090] = 0xf9;
            agariTable[0x20836c0080] = 0xf9;
            agariTable[0x26d8000100] = 0xf9;
            agariTable[0x6d8100400] = 0xf9;
            agariTable[0x36c0002100] = 0xf9;
            agariTable[0x2000] = 0x24;
            agariTable[0x20004d8000] = 0xa9;
            agariTable[0xdb410080] = 0xf9;
            agariTable[0x3620000000] = 0xa9;
            agariTable[0x21036c0] = 0xf9;
            agariTable[0x20db010080] = 0xf9;
            agariTable[0x21036c0000] = 0xf9;
            agariTable[0x20020db400] = 0xf9;
            agariTable[0x806da400] = 0xf9;
            agariTable[0x24036c0400] = 0xf9;
            agariTable[0x3604000] = 0xa9;
            agariTable[0x6da002010] = 0xf9;
            agariTable[0x92480] = 0xa5;
            agariTable[0x28db000] = 0xf9;
            agariTable[0x41b610080] = 0xf9;
            agariTable[0x6d8002410] = 0xf9;
            agariTable[0x6c2000080] = 0xa9;
            agariTable[0x2002003600] = 0xa9;
            agariTable[0x3700] = 0xa9;
            agariTable[0x2000083600] = 0xa9;
            agariTable[0x20db100] = 0xf9;
            agariTable[0x806c2000] = 0xa9;
            agariTable[0x136c2400] = 0xf9;
            agariTable[0x800126d8] = 0xf9;
            agariTable[0xdb600] = 0xc8;
            agariTable[0x6c0410] = 0xa9;
            agariTable[0x1b604010] = 0xf9;
            agariTable[0x4d8080000] = 0xa9;
            agariTable[0x26d8082000] = 0xf9;
            agariTable[0x36c0420000] = 0xf9;
            agariTable[0x10000] = 0x24;
            agariTable[0x201001b610] = 0xf9;
            agariTable[0x36db000000] = 0xf8;
            agariTable[0x201b690] = 0xf9;
            agariTable[0x26d8800] = 0xf9;
            agariTable[0x20206d8000] = 0xf9;
            agariTable[0x600000] = 0x36;
            agariTable[0x6da410] = 0xf9;
            agariTable[0x20006c2000] = 0xa9;
            agariTable[0x4036c0020] = 0xf9;
            agariTable[0x403602000] = 0xa9;
            agariTable[0x36c0090010] = 0xf9;
            agariTable[0x49240000] = 0x51;
            agariTable[0x4136d0] = 0xf9;
            agariTable[0x800003600] = 0xa9;
            agariTable[0x6d8800080] = 0xf9;
            agariTable[0x6d8404000] = 0xf9;
            agariTable[0x20020106d8] = 0xf9;
            agariTable[0x4820006d8] = 0xf9;
            agariTable[0x20900006d8] = 0xf9;
            agariTable[0x8201b680] = 0xf9;
            agariTable[0x4036c0410] = 0xf9;
            agariTable[0x4db080010] = 0xf9;
            agariTable[0x6db600] = 0xf8;
            agariTable[0x81001b600] = 0xf9;
            agariTable[0x4004db080] = 0xf9;
            agariTable[0x4806d8010] = 0xf9;
            agariTable[0x20006da080] = 0xf9;
            agariTable[0x201b602080] = 0xf9;
            agariTable[0x11b600080] = 0xf9;
            agariTable[0x1b000410] = 0xa9;
            agariTable[0x13000000] = 0x57;
            agariTable[0x26d0] = 0xa9;
            agariTable[0x49b680] = 0xf9;
            agariTable[0x24806d8000] = 0xf9;
            agariTable[0x4236c0000] = 0xf9;
            agariTable[0x2400000000] = 0x45;
            agariTable[0x401b680] = 0xf9;
            agariTable[0x8001b010] = 0xa9;
            agariTable[0x4006d8800] = 0xf9;
            agariTable[0xdb000880] = 0xf9;
            agariTable[0x6d0000080] = 0xa9;
            agariTable[0x104006c0] = 0xa9;
            agariTable[0x800136d0] = 0xf9;
            agariTable[0x36c2090000] = 0xf9;
            agariTable[0x226d8] = 0xf9;
            agariTable[0x806dc000] = 0xf9;
            agariTable[0x36d0400080] = 0xf9;
            agariTable[0x20836d0] = 0xf9;
            agariTable[0x28000db000] = 0xf9;
            agariTable[0x4db000090] = 0xf9;
            agariTable[0x20000c0000] = 0x57;
            agariTable[0x4106c0000] = 0xa9;
            agariTable[0x26d8090000] = 0xf9;
            agariTable[0x4120006d8] = 0xf9;
            agariTable[0x4040db000] = 0xf9;
            agariTable[0x36d2400] = 0xf9;
            agariTable[0x41b600480] = 0xf9;
            agariTable[0x201009b600] = 0xf9;
            agariTable[0x2492480] = 0xe5;
            agariTable[0x20000800d8] = 0xa9;
            agariTable[0x106c0010] = 0xa9;
            agariTable[0x836c0090] = 0xf9;
            agariTable[0x249249000] = 0x81;
            agariTable[0x2083600000] = 0xa9;
            agariTable[0x1b082000] = 0xa9;
            agariTable[0xda000010] = 0xa9;
            agariTable[0x3680080000] = 0xa9;
            agariTable[0x80000000] = 0x24;
            agariTable[0xa06d8] = 0xf9;
            agariTable[0x4020806d8] = 0xf9;
            agariTable[0xdb420] = 0xf9;
            agariTable[0x36c0002410] = 0xf9;
            agariTable[0x249240] = 0x61;
            agariTable[0x20006d8100] = 0xf9;
            agariTable[0x3690000] = 0xa9;
            agariTable[0x3600010010] = 0xa9;
            agariTable[0x3600020000] = 0xa9;
            agariTable[0x1000026d8] = 0xf9;
            agariTable[0x4900036c0] = 0xf9;
            agariTable[0x3602000080] = 0xa9;
            agariTable[0x602000000] = 0x57;
            agariTable[0x20006da400] = 0xf9;
            agariTable[0x24026d8000] = 0xf9;
            agariTable[0x826c0000] = 0xa9;
            agariTable[0xd8010080] = 0xa9;
            agariTable[0x8001b700] = 0xf9;
            agariTable[0x8209b600] = 0xf9;
            agariTable[0x3700000] = 0xa9;
            agariTable[0x20db000480] = 0xf9;
            agariTable[0x4db400080] = 0xf9;
            agariTable[0x18010000] = 0x57;
            agariTable[0x24006c0] = 0xa9;
            agariTable[0x2003680000] = 0xa9;
            agariTable[0x41b610010] = 0xf9;
            agariTable[0xdb800010] = 0xf9;
            agariTable[0x4000c0] = 0x57;
            agariTable[0x126d8400] = 0xf9;
            agariTable[0x36e0010] = 0xf9;
            agariTable[0x800000] = 0x42;
            agariTable[0x12490] = 0xa5;
            agariTable[0x24000036d0] = 0xf9;
            agariTable[0x41001b610] = 0xf9;
            agariTable[0x36c0090400] = 0xf9;
            agariTable[0x900006c0] = 0xa9;
            agariTable[0x600400000] = 0x57;
            agariTable[0x8036d0000] = 0xf9;
            agariTable[0x6d8880000] = 0xf9;
            agariTable[0x90000000] = 0x45;
            agariTable[0xd8400400] = 0xa9;
            agariTable[0x26d8002010] = 0xf9;
            agariTable[0x36c2090] = 0xf9;
            agariTable[0x8004006d8] = 0xf9;
            agariTable[0x120db010] = 0xf9;
            agariTable[0x26c2000] = 0xa9;
            agariTable[0x6da100000] = 0xf9;
            agariTable[0x26dc000000] = 0xf9;
            agariTable[0x20100106d8] = 0xf9;
            agariTable[0x6dc400] = 0xf9;
            agariTable[0x1b010010] = 0xa9;
            agariTable[0x9b000400] = 0xa9;
            agariTable[0x6da000090] = 0xf9;
            agariTable[0x36d0002010] = 0xf9;
            agariTable[0x36d0012000] = 0xf9;
            agariTable[0x3002000000] = 0x57;
            agariTable[0x4026d8080] = 0xf9;
            agariTable[0x26dc000] = 0xf9;
            agariTable[0x280001b600] = 0xf9;
            agariTable[0x4806c0000] = 0xa9;
            agariTable[0xd8080080] = 0xa9;
            agariTable[0x36c2080010] = 0xf9;
            agariTable[0x800] = 0x42;
            agariTable[0x4000da000] = 0xa9;
            agariTable[0x6d0400] = 0xa9;
            agariTable[0x36c0002090] = 0xf9;
            agariTable[0x26d8410] = 0xf9;
            agariTable[0x40006c0] = 0xa9;
            agariTable[0x100db410] = 0xf9;
            agariTable[0x806c0080] = 0xa9;
            agariTable[0x103600000] = 0xa9;
            agariTable[0x8806d8] = 0xf9;
            agariTable[0x4000826d8] = 0xf9;
            agariTable[0xdb110000] = 0xf9;
            agariTable[0x26c0000080] = 0xa9;
            agariTable[0x400003000] = 0x57;
            agariTable[0x4020136c0] = 0xf9;
            agariTable[0x28036c0] = 0xf9;
            agariTable[0x4db800000] = 0xf9;
            agariTable[0x4906d8] = 0xf9;
            agariTable[0x83000000] = 0x57;
            agariTable[0x10003610] = 0xa9;
            agariTable[0x4800db010] = 0xf9;
            agariTable[0x106da010] = 0xf9;
            agariTable[0x20106c0000] = 0xa9;
            agariTable[0x8006c0000] = 0xa9;
            agariTable[0x20036c0480] = 0xf9;
            agariTable[0x800c0] = 0x57;
            agariTable[0x1b610090] = 0xf9;
            agariTable[0x20db020000] = 0xf9;
            agariTable[0x9b600020] = 0xf9;
            agariTable[0x483600] = 0xa9;
            agariTable[0x49249240] = 0x91;
            agariTable[0x920db000] = 0xf9;
            agariTable[0x4826d8000] = 0xf9;
            agariTable[0x1b604080] = 0xf9;
            agariTable[0x4024036c0] = 0xf9;
            agariTable[0x206d8080] = 0xf9;
            agariTable[0x8006da000] = 0xf9;
            agariTable[0xd8080010] = 0xa9;
            agariTable[0x41001b680] = 0xf9;
            agariTable[0x1000036d0] = 0xf9;
            agariTable[0x100d8010] = 0xa9;
            agariTable[0x41b080000] = 0xa9;
            agariTable[0x4008006d8] = 0xf9;
            agariTable[0x826c0] = 0xa9;
            agariTable[0x4020006c0] = 0xa9;
            agariTable[0x40136c0] = 0xf9;
            agariTable[0x900db080] = 0xf9;
            agariTable[0xc0] = 0x36;
            agariTable[0x241b600400] = 0xf9;
            agariTable[0x6d8002020] = 0xf9;
            agariTable[0x20024036c0] = 0xf9;
            agariTable[0x48001b680] = 0xf9;
            agariTable[0x20036c0410] = 0xf9;
            agariTable[0x209b610000] = 0xf9;
            agariTable[0x36c0010480] = 0xf9;
            agariTable[0x403000000] = 0x57;
            agariTable[0x24100036c0] = 0xf9;
            agariTable[0x90003600] = 0xa9;
            agariTable[0x208201b600] = 0xf9;
            agariTable[0x106d8090] = 0xf9;
            agariTable[0x4006da400] = 0xf9;
            agariTable[0xd8002010] = 0xa9;
            agariTable[0xd8082000] = 0xa9;
            agariTable[0x24d8] = 0xa9;
            agariTable[0x24000000d8] = 0xa9;
            agariTable[0x20906d8] = 0xf9;
            agariTable[0x40d8000] = 0xa9;
            agariTable[0x6d8482000] = 0xf9;
            agariTable[0x6c2000400] = 0xa9;
            agariTable[0x3000400000] = 0x57;
            agariTable[0x206c0] = 0xa9;
            agariTable[0x820836c0] = 0xf9;
            agariTable[0x136d0010] = 0xf9;
            agariTable[0x6c0010010] = 0xa9;
            agariTable[0x6db600000] = 0xf8;
            agariTable[0x200001b700] = 0xf9;
            agariTable[0x24004db000] = 0xf9;
            agariTable[0x36c0400100] = 0xf9;
            agariTable[0x8000006c0] = 0xa9;
            agariTable[0x900806d8] = 0xf9;
            agariTable[0x120db080] = 0xf9;
            agariTable[0x6d8880] = 0xf9;
            agariTable[0x836c0410] = 0xf9;
            agariTable[0xdb080020] = 0xf9;
            agariTable[0x36c0080410] = 0xf9;
            agariTable[0x40806d8] = 0xf9;
            agariTable[0x2000600] = 0x57;
            agariTable[0x200000000] = 0x13;
            agariTable[0xdb002410] = 0xf9;
            agariTable[0x28db000000] = 0xf9;
            agariTable[0x6dc080000] = 0xf9;
            agariTable[0x2201b600] = 0xf9;
            agariTable[0x4000236c0] = 0xf9;
            agariTable[0x13610] = 0xa9;
            agariTable[0x3600100] = 0xa9;
            agariTable[0x49b000000] = 0xa9;
            agariTable[0xd8012000] = 0xa9;
            agariTable[0x36c2010400] = 0xf9;
            agariTable[0x4004026d8] = 0xf9;
            agariTable[0x800826d8] = 0xf9;
            agariTable[0x836e0] = 0xf9;
            agariTable[0x4db010080] = 0xf9;
            agariTable[0xda000400] = 0xa9;
            agariTable[0xc0000000] = 0x36;
            agariTable[0x46d8400] = 0xf9;
            agariTable[0x6d0010] = 0xa9;
            agariTable[0x36c0810] = 0xf9;
            agariTable[0x6c0000800] = 0xa9;
            agariTable[0x36c00000a0] = 0xf9;
            agariTable[0x400018] = 0x57;
            agariTable[0x18400] = 0x57;
            agariTable[0x4c0000000] = 0x57;
            agariTable[0x8081b600] = 0xf9;
            agariTable[0x28036c0000] = 0xf9;
            agariTable[0x1b690010] = 0xf9;
            agariTable[0x9b680080] = 0xf9;
            agariTable[0x26d8080080] = 0xf9;
            agariTable[0x1b6c0000] = 0xc8;
            agariTable[0x3600000090] = 0xa9;
            agariTable[0x200041b680] = 0xf9;
            agariTable[0x20db080080] = 0xf9;
            agariTable[0x36c2012000] = 0xf9;
            agariTable[0x24004036c0] = 0xf9;
            agariTable[0x20006d8020] = 0xf9;
            agariTable[0x3680002000] = 0xa9;
            agariTable[0x4004806d8] = 0xf9;
            agariTable[0x900db400] = 0xf9;
            agariTable[0x4db000100] = 0xf9;
            agariTable[0xd8400010] = 0xa9;
            agariTable[0x20d8080000] = 0xa9;
            agariTable[0x208001b610] = 0xf9;
            agariTable[0x20826d8000] = 0xf9;
            agariTable[0x8806d8000] = 0xf9;
            agariTable[0x24db002000] = 0xf9;
            agariTable[0x36e0002000] = 0xf9;
            agariTable[0x24106d8] = 0xf9;
            agariTable[0x4004136c0] = 0xf9;
            agariTable[0x200209b600] = 0xf9;
            agariTable[0x1009b000] = 0xa9;
            agariTable[0x11b600400] = 0xf9;
            agariTable[0xd8010010] = 0xa9;
            agariTable[0xd8400080] = 0xa9;
            agariTable[0x26d8010010] = 0xf9;
            agariTable[0x1249249200] = 0xa1;
            agariTable[0x846d8000] = 0xf9;
            agariTable[0x492492000] = 0x105;
            agariTable[0xdb020400] = 0xf9;
            agariTable[0x120000d8] = 0xa9;
            agariTable[0x36e0080000] = 0xf9;
            agariTable[0x241b610] = 0xf9;
            agariTable[0x6c0800] = 0xa9;
            agariTable[0x3000000] = 0x36;
            agariTable[0x906d8080] = 0xf9;
            agariTable[0x1b612080] = 0xf9;
            agariTable[0x36c0012400] = 0xf9;
            agariTable[0x3602000010] = 0xa9;
            agariTable[0x1b090] = 0xa9;
            agariTable[0x4100d8000] = 0xa9;
            agariTable[0x6da090] = 0xf9;
            agariTable[0x20106d8010] = 0xf9;
            agariTable[0x4046d8000] = 0xf9;
            agariTable[0x8106d8] = 0xf9;
            agariTable[0x20120006d8] = 0xf9;
            agariTable[0x6c0010080] = 0xa9;
            agariTable[0x36d0000100] = 0xf9;
            agariTable[0x680] = 0x57;
            agariTable[0x1b000] = 0x68;
            agariTable[0x600000080] = 0x57;
            agariTable[0x4120036c0] = 0xf9;
            agariTable[0x200db010] = 0xf9;
            agariTable[0x26da080] = 0xf9;
            agariTable[0x4136c0080] = 0xf9;
            agariTable[0x403680000] = 0xa9;
            agariTable[0x400000000] = 0x24;
            agariTable[0x6d8102000] = 0xf9;
            agariTable[0x6c0020000] = 0xa9;
            agariTable[0x36c0000490] = 0xf9;
            agariTable[0x241b680000] = 0xf9;
            agariTable[0x800db410] = 0xf9;
            agariTable[0x5000db000] = 0xf9;
            agariTable[0x1b800000] = 0xa9;
            agariTable[0x24db000080] = 0xf9;
            agariTable[0x6d8410080] = 0xf9;
            agariTable[0x26d8000800] = 0xf9;
            agariTable[0x26c0080000] = 0xa9;
            agariTable[0x120106d8] = 0xf9;
            agariTable[0x24800036c0] = 0xf9;
            agariTable[0x4d8000010] = 0xa9;
            agariTable[0x900000d8] = 0xa9;
            agariTable[0x220db000] = 0xf9;
            agariTable[0x36c2100] = 0xf9;
            agariTable[0x81b600400] = 0xf9;
            agariTable[0x26d8400400] = 0xf9;
            agariTable[0x1249000] = 0x51;
            agariTable[0x20000db410] = 0xf9;
            agariTable[0x20026c0000] = 0xa9;
            agariTable[0x20036c0100] = 0xf9;
            agariTable[0x1b702000] = 0xf9;
            agariTable[0x20db080400] = 0xf9;
            agariTable[0x6d8810000] = 0xf9;
            agariTable[0x4000906d8] = 0xf9;
            agariTable[0x8201b000] = 0xa9;
            agariTable[0x20100db400] = 0xf9;
            agariTable[0x4006d8410] = 0xf9;
            agariTable[0x20000906d8] = 0xf9;

        }
    }
}
