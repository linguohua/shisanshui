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

	sort.Slice(slotitems, func(i, j int) bool {
		cnt1 := len(slotitems[i].cards)
		cnt2 := len(slotitems[j].cards)
		return cnt1 > cnt2
	})

	tag := int64(0)
	for i := 0; i < len(slotitems); i-- {
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
		sum = sum + v
	}

	return sum == 1
}

// patternVerifyStraight 检查是否顺子
func patternVerifyStraight(hai []int32) bool {
	var i = 0

	if hai[0]/4 == int32(xproto.CardID_AC)/4 && hai[1]/4 == int32(xproto.CardID_R5C)/4 {
		// A, 5, ...
		i = 1
	}

	for ; i < len(hai)-1; i++ {
		if hai[i]-hai[i+1] != 1 {
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

	// 牌大到小排列
	sort.Slice(hai, func(i, j int) bool {
		rankI := hai[i] / 4
		rankJ := hai[j] / 4
		if rankI == rankJ {
			return hai[i] > hai[j]
		}

		return rank2Priority[rankI] > rank2Priority[rankJ]
	})

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

	cl.Printf("convertMsgCardHand, agarix:%x, ct:%d\n", agari, ct)

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

func init() {
	patternTable[0x20] = 0x503
	patternTable[0x137] = 0x506
	patternTable[0xdd] = 0x507
	patternTable[0x83f] = 0x508
	patternTable[0x6f] = 0x10309
	patternTable[0x3] = 0x306
	patternTable[0x15] = 0x308
	patternTable[0x2b67] = 0x10509
	patternTable[0x29] = 0x502
}
