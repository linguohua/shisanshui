// 牌型
// 计算每一墩牌的牌型

package tables

import (
	"sort"

	"shisanshui/xproto"

	"github.com/sirupsen/logrus"
)

var (
	patternTable = make(map[int64]int)

	// 2,  3,  4,  5,  6,  7,  8,  9,  10, J,  Q,  K,  A
	// 0   1   2   3   4   5   6   7   8   9   10  11  12
	// 0   1   2   3   4   5   6   7   8   9   10  11  12
	rank2Priority = []int{0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12}
	priority2Rank = []int{0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12}
)

type slotitem struct {
	cards []int32
}

// calcPatternKey 计算key以及抽取slots
func calcPatternKey(hai []int32, cl *logrus.Entry) (int64, []*slotitem) {
	// 最多14种牌
	slotitems := make([]*slotitem, 14)
	for i := range slotitems {
		slotitems[i] = &slotitem{}
	}

	for _, h := range hai {
		item := slotitems[h/4]
		item.cards = append(item.cards, h)
	}

	for _, s := range slotitems {
		if len(s.cards) > 4 {
			cl.Panicln("slots elem great than 3:", s)
		}
	}

	//cl.Printf("before sort slotitems:%+v", slotitems)
	sort.Slice(slotitems, func(i, j int) bool {
		cnt1 := len(slotitems[i].cards)
		cnt2 := len(slotitems[j].cards)
		return cnt1 > cnt2
	})
	//cl.Printf("after sort slotitems:%+v", slotitems)

	tag := int64(0)
	for i := 0; i < len(slotitems); i++ {
		l := len(slotitems[i].cards)
		if l == 0 {
			break
		}

		tag = tag*10 + int64(l)
	}

	return tag, slotitems
}

// patternVerifyFlush 检查是否同花
func patternVerifyFlush(hai []int32) bool {
	slots := make([]int, 4)

	for _, h := range hai {
		slots[h%4]++
	}

	sum := 0
	for _, v := range slots {
		if v > 0 {
			sum++
		}
	}

	return sum == 1
}

// patternVerifyStraight 检查是否顺子
func patternVerifyStraight(hai []int32) bool {
	var i = 0
	//墩子的顺子 可能是 A23、A2345、QKA、10JQKA (所以应该是判断最后一张是不是2)
	if hai[0]/4 == int32(xproto.CardID_AC)/4 && hai[len(hai)-1]/4 == int32(xproto.CardID_R2C)/4 {
		// A, 5, ...
		i = 1
	}

	for ; i < len(hai)-1; i++ {
		if hai[i]/4-hai[i+1]/4 != 1 {
			return false
		}
	}

	return true
}

// patternConvertMsgCardHand 转换为MsgCardHand
func patternConvertMsgCardHand(hai []int32, cl *logrus.Entry) *xproto.MsgCardHand {
	if len(hai) < 3 {
		cl.Panicf("hand cards count should >= 3, current:%d", len(hai))
	}

	//cl.Printf("before sort Slice:%+v", hai)
	// 牌大到小排列
	sort.Slice(hai, func(i, j int) bool {
		rankI := hai[i] / 4
		rankJ := hai[j] / 4
		if rankI == rankJ {
			return hai[i] > hai[j]
		}

		return rank2Priority[rankI] > rank2Priority[rankJ]
	})
	//cl.Printf("after sort Slice:%+v", hai)

	key, slots := calcPatternKey(hai, cl)

	agari, ok := patternTable[key]
	if !ok {
		cl.Println("invalid hai")
		return nil
	}

	ct := xproto.CardHandType(agari & 0x00ff)
	straightAble := (agari >> 16) & 0x00ff

	var isFlush = patternVerifyFlush(hai)

	// straightSlots := make([]bool, len(slots))
	var isStraight = false
	// 如果是顺子
	if straightAble > 0 {
		isStraight = patternVerifyStraight(hai)
	}

	cl.Printf("convertMsgCardHand, agarix:%x, ct:%d", agari, ct)

	if isStraight {
		// 如果是顺子，则不可能是4张，也不可能是葫芦，因此要么作为同花顺，要么作为顺子
		if isFlush {
			ct = xproto.CardHandType_StraightFlush
		} else {
			ct = xproto.CardHandType_Straight
		}
	} else if isFlush {
		// 如果是同花，此时如果牌型比同花小，则修改为同花
		if ct < xproto.CardHandType_Flush {
			ct = xproto.CardHandType_Flush
		}
	}

	cardHand := &xproto.MsgCardHand{}
	var cardHandType32 = int32(ct)
	cardHand.CardHandType = &cardHandType32

	haiNew := make([]int32, 0, len(hai)+1)

	if len(slots[0].cards) > 1 {
		// 张数从多到少排列
		for _, v := range slots {
			if v.cards == nil {
				break
			}

			haiNew = append(haiNew, v.cards...)
		}
	} else {

		haiNew = append(haiNew, hai...)
	}

	cardHand.Cards = haiNew
	return cardHand
}

// calc13 特殊牌型转换为MsgCardHand
func calc13(hai []int32, cl *logrus.Entry) *xproto.MsgCardHand {
	if len(hai) != 13 {
		cl.Panicf("hand cards count should == 13, current:%d", len(hai))
	}

	//保存一份用于判断三顺子三同花
	oldHai := make([]int32, 13)
	for i := 0; i < 13; i++ {
		oldHai[i] = hai[i]
	}

	//cl.Printf("calc13, before sort Slice:%+v", hai)
	// 牌大到小排列
	sort.Slice(hai, func(i, j int) bool {
		rankI := hai[i] / 4
		rankJ := hai[j] / 4
		if rankI == rankJ {
			return hai[i] > hai[j]
		}

		return rank2Priority[rankI] > rank2Priority[rankJ]
	})

	//cl.Printf("calc13, after sort Slice:%+v", hai)
	cardNums := make([]int, 14) //保存每种牌个数
	slots := make([]int, 4)     //花色数量 0:红桃 1:方块 2:梅花 3:黑桃
	isStraight := true          //是否是顺子
	for i := 0; i < len(hai); i++ {
		card1 := hai[i]
		if i+1 < len(hai) {
			card2 := hai[i+1]
			if card1/4-card2/4 != 1 {
				isStraight = false
			}
		}
		slots[card1%4]++
		cardNums[card1/4]++
	}
	sum := 0      //有几种花色
	redNum := 0   //红牌张数
	blackNum := 0 //黑牌张数
	for i, v := range slots {
		if v > 0 {
			sum++
		}
		if i == 0 || i == 1 {
			redNum += v
		}
		if i == 2 || i == 3 {
			blackNum += v
		}
	}
	ct := xproto.SpecialType_Special_None
	if isStraight {
		ct = xproto.SpecialType_All_Straight
		if sum == 1 {
			ct = xproto.SpecialType_All_StraightFlush
		}
	} else {
		if sum == 1 {
			//一种花色
			ct = xproto.SpecialType_Pure_One_Suit
		} else if redNum == 1 {
			//其中一种颜色只有1张  就是一点黑或者一点红
			ct = xproto.SpecialType_One_Red
		} else if blackNum == 1 {
			ct = xproto.SpecialType_One_Black
		} else {
			pairsNum := 0        //对子个数
			threeOfAKindNum := 0 //三条个数
			for _, c := range cardNums {
				if c == 2 {
					pairsNum++
				} else if c == 3 {
					threeOfAKindNum++
				}
			}
			//5对1三条
			if pairsNum == 5 && threeOfAKindNum == 1 {
				ct = xproto.SpecialType_FivePairs_ThreeOfAKind
			}
			//6对半
			if pairsNum == 6 {
				ct = xproto.SpecialType_SixPairs_HighCard
			}
		}
	}
	if ct == xproto.SpecialType_Special_None {
		//不是上面的牌型就看看是不是三同花跟三顺子
		hands := [][]int32{oldHai[0:5], oldHai[5:10], oldHai[10:13]}
		fNum := 0 //同花墩数
		sNum := 0 //顺子墩数
		for _, hand := range hands {
			if patternVerifyStraight(hand) {
				sNum++
			}
			if patternVerifyFlush(hand) {
				fNum++
			}
		}
		if sNum == 3 {
			ct = xproto.SpecialType_Three_Straight
		}
		if fNum == 3 {
			ct = xproto.SpecialType_Three_Flush
		}
		hai = oldHai
	}

	cardHand := &xproto.MsgCardHand{}
	var cardHandType32 = int32(ct)
	cardHand.CardHandType = &cardHandType32
	cardHand.Cards = hai
	return cardHand
}

func init() {
	patternTable[0x2b67] = 0x10501
	patternTable[0x29] = 0x508
	patternTable[0x83f] = 0x502
	patternTable[0x6f] = 0x10301
	patternTable[0x3] = 0x304
	patternTable[0x15] = 0x302
	patternTable[0x20] = 0x507
	patternTable[0x137] = 0x504
	patternTable[0xdd] = 0x503
}
