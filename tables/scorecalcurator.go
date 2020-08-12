// 计算每一个玩家的输赢得分

package tables

import "shisanshui/xproto"

var (
	//牌型对应得分
	scoreOfCardType = map[xproto.CardHandType]int{
		//普通牌型
		// 同花顺	Straight Flush 五张或更多的连续单牌（如： 45678 或 78910JQK ）
		xproto.CardHandType_StraightFlush: 9,
		// 四条 Four of a Kind：四张同点牌 + 一张
		xproto.CardHandType_Four: 9,
		// 葫芦
		xproto.CardHandType_FullHouse: 9,
		// 同花(花)	Flush
		xproto.CardHandType_Flush: 9,
		// 顺子(蛇)	Straight
		xproto.CardHandType_Straight: 9,
		// 三条 Three of a kind
		xproto.CardHandType_ThreeOfAKind: 9,
		// 两对牌：数值相同的两张牌
		xproto.CardHandType_TwoPairs: 9,
		// 对牌
		xproto.CardHandType_OnePair: 9,
		// 单张
		xproto.CardHandType_HighCard: 9,
		//特殊牌型
		// 三顺子 每一墩都是顺子（如  23456、45678、789）
		xproto.CardHandType_Three_Straight: 9,
		// 三同花 每一墩花色相同（如 第一墩都是梅花 第二墩都是方块）
		xproto.CardHandType_Three_Flush: 9,
		// 六对半
		xproto.CardHandType_SixPairs_HighCard: 9,
		// 五对 加 三条
		xproto.CardHandType_FivePairs_ThreeOfAKind: 9,
		// 一点黑
		xproto.CardHandType_One_Black: 9,
		// 一点红
		xproto.CardHandType_One_Red: 9,
		// 清一色 (全黑或全红 可以方块红桃混合)
		xproto.CardHandType_Pure_One_Suit: 9,
		// 一条龙
		xproto.CardHandType_All_Straight: 9,
		// 至尊清龙
		xproto.CardHandType_All_StraightFlush: 9,
	}
)

func calcFinalResult(s *statePlaying) {
	//计算结果
	for _, p := range s.playingPlayers {
		//TODO ：判断是否 倒墩(弃权) 是的话就不参与比较
		cardHand := patternConvertMsgCardHand(p.hcontext.sortCards, p.cl)
		p.hcontext.specialCardType = *cardHand.CardHandType
		if *cardHand.CardHandType != int32(xproto.CardHandType_None) {
			//有特殊牌型
			p.hcontext.specialCardNum = cardHand.Cards[0] //此处要保证最前面的牌是最大的
		} else {
			//没有特殊牌型 要每一墩都计算
			if len(p.hcontext.sortCards) == 13 {
				p.hcontext.cardDuns = make([]*xproto.MsgPlayerScoreDun, 3)

				dun1 := p.hcontext.sortCards[0:5]
				dun2 := p.hcontext.sortCards[5:10]
				dun3 := p.hcontext.sortCards[10:13]
				caleAndSaveScoreDun(1, dun1, p)
				caleAndSaveScoreDun(2, dun2, p)
				caleAndSaveScoreDun(3, dun3, p)
			}
		}
	}
	//比较大小
	for _, myP := range s.playingPlayers {
		for _, otherP := range s.playingPlayers {
			//跟其他人比较 输赢只记录自己的就可以
			if myP != otherP {
				if myP.hcontext.specialCardType != otherP.hcontext.specialCardType {
					//先比较特殊牌型（这里还要考虑 相同的特殊牌型）

				}

			}
		}
	}
}

func comparePlayerResult(p1 *Player, p2 *Player) {

}

func caleAndSaveScoreDun(dun int32, cards []int32, p *Player) {
	cardHand := patternConvertMsgCardHand(cards, p.cl)
	sd := &xproto.MsgPlayerScoreDun{}
	sd.CardType = cardHand.CardHandType
	sd.CardNum = &cardHand.Cards[0] //此处要保证最前面的牌是最大的
	sd.Dun = &dun

	p.hcontext.cardDuns[dun-1] = sd
}
