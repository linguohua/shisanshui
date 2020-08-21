using System;
using System.Collections.Generic;
using Xproto;

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
        /// <returns>如果牌列表是一个有效的组合，则返回一个MsgCardHand对象，否则返回null</returns>
        public static MsgCardHand  agariConvertMsgCardHand(int[] hai)
        {
            var key = calcKey(hai);
            if (!agariTable.ContainsKey(key))
            {
                return null;
            }

            var agari = agariTable[key];
            var ct = (CardHandType)(agari & 0x0f);

            var msgCardhand = new MsgCardHand();
            msgCardhand.CardHandType = (int)ct;

            // 排序，让大的牌在前面
            Array.Sort(hai, (x, y) =>
            {
                return y - x;
            });

            var cardsNew = new List<int>();
            switch (ct)
            {
                case CardHandType.Flush:
                    cardsNew.AddRange(hai);
                    // 对于ACE需要特殊考虑，如果ACE作为类似于12345这样的顺子
                    var isAceSmallest = ((agari & 0x0100) != 0);
                    if (isAceSmallest) {
                        var swp = cardsNew[0];
                        cardsNew.RemoveAt(0);
                        cardsNew.Add(swp);
                    }
                    break;

                //case CardHandType.TripletPair:
                //case CardHandType.Triplet2X2Pair:
                    // 确保3张在前面，对子在后面
                    //for (var i = 0; i < hai.Length; i++)
                    //{
                    //    var h = hai[i];
                    //    if (slots[h/4] == 3)
                    //    {
                    //        cardsNew.Add(h);
                    //    }
                    //}
                    //for (var i = 0; i < hai.Length; i++)
                    //{
                    //    var h = hai[i];
                    //    if (slots[h / 4] != 3)
                    //    {
                    //        cardsNew.Add(h);
                    //    }
                    //}
                    break;
                default:
                    cardsNew.AddRange(hai);
                    break;
            }

            msgCardhand.Cards.AddRange(cardsNew);

            // 如果是3个3，而且不包含红桃3，则把牌组改为炸弹，而不是三张
            //if (ct == CardHandType.Triplet && msgCardhand.cards[0]/4 == (int)CardID.R3H/4)
            //{
            //    var foundR3H = false;
            //    foreach (var c in msgCardhand.cards)
            //    {
            //        if (c == (int)CardID.R3H)
            //        {
            //            foundR3H = true;
            //            break;
            //        }
            //    }

            //    if (!foundR3H)
            //    {
            //        msgCardhand.cardHandType = (int)CardHandType.Bomb;
            //    }
            //}

            return msgCardhand;
        }

        /// <summary>
        /// 判断当前的手牌是否大于上一手牌
        /// </summary>
        /// <param name="prevCardHand">上一手牌</param>
        /// <param name="current">当前的手牌</param>
        /// <returns>如果大于则返回true，其他各种情形都会返回false</returns>
        public static bool agariGreatThan(MsgCardHand prevCardHand, MsgCardHand current)
        {
            // 如果当前的是炸弹
            //if (current.cardHandType == (int)CardHandType.Bomb)
            //{
            //    // 上一手不是炸弹
            //    if (prevCardHand.cardHandType != (int)CardHandType.Bomb)
            //    {
            //        return true;
            //    }

            //    // 上一手也是炸弹，则比较炸弹牌的大小，大丰关张不存在多于4个牌的炸弹
            //    return current.cards[0]/4 > prevCardHand.cards[0]/4;
            //}

            //// 如果上一手牌是炸弹
            //if (prevCardHand.cardHandType == (int)CardHandType.Bomb)
            //{
            //    return false;
            //}

            //// 必须类型匹配
            //if (prevCardHand.cardHandType != current.cardHandType)
            //{
            //    return false;
            //}

            //// 张数匹配
            //if (prevCardHand.cards.Count != current.cards.Count)
            //{
            //    return false;
            //}

            //// 单张时，2是最大的
            //if (prevCardHand.cardHandType == (int)CardHandType.Single)
            //{
            //    if (prevCardHand.cards[0] / 4 == 0)
            //    {
            //        return false;

            //    }

            //    if (current.cards[0] / 4 == 0)
            //    {
            //        return true;
            //    }
            //}

            //// 现在只比较最大牌的大小
            //return current.cards[0]/4 > prevCardHand.cards[0]/4;

            return false;
        }

    }
}
