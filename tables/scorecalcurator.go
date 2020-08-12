// 计算每一个玩家的输赢得分

package tables

import "shisanshui/xproto"

var (
	//牌型对应得分
	scoreOfCardType = map[xproto.CardHandType]int32{
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

	//保存已经比较过的玩家
	comparePlayers [][]int
)

func calcFinalResult(s *statePlaying) {
	comparePlayers = make([][]int, 0)
	//计算结果
	for _, p := range s.playingPlayers {
		p.hcontext.cardDuns = make(map[int32]*xproto.MsgPlayerScoreDun)
		//TODO ：判断是否 倒墩(弃权) 是的话就不参与比较
		cardHand := patternConvertMsgCardHand(p.hcontext.sortCards, p.cl)
		if cardHand.GetCardHandType() != int32(xproto.CardHandType_None) {
			//有特殊牌型
			sd := &xproto.MsgPlayerScoreDun{}
			sd.CardType = cardHand.CardHandType
			sd.CardNum = &cardHand.Cards[0] //此处要保证最前面的牌是最大的
			dun := int32(0)
			sd.Dun = &dun
			p.hcontext.cardDuns[dun] = sd
		} else {
			//没有特殊牌型 要每一墩都计算
			if len(p.hcontext.sortCards) == 13 {
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
			if myP != otherP {
				comparePlayerResult(myP, otherP)
			}
		}
	}
}

func comparePlayerScoreDun(dun int32, ps1, ps2 *xproto.MsgPlayerScoreDun, p1, p2 *Player) bool {
	if ps1 != nil && ps2 != nil {
		//先比较特殊牌型
		ct1 := ps1.GetCardType()
		ct2 := ps2.GetCardType()
		if ct1 > ct2 {
			saveScoreDunInfo(dun, p1, p2)
		} else if ct1 < ct2 {
			saveScoreDunInfo(dun, p2, p1)
		} else {
			//这里还要考虑 相同的牌型
		}
	} else if ps1 != nil {
		saveScoreDunInfo(dun, p1, p2)
	} else if ps2 != nil {
		saveScoreDunInfo(dun, p2, p1)
	} else {
		//两个都为空 则没法比较 返回false
		return false
	}
	return true
}

func comparePlayerResult(p1 *Player, p2 *Player) {
	for _, cs := range comparePlayers {
		chair1 := cs[0]
		chair2 := cs[1]
		if (p1.chairID == chair1 && p2.chairID == chair2) ||
			(p1.chairID == chair2 && p2.chairID == chair1) {
			//之前比较过 不在比较
			return
		}
	}
	//先保存进数组 避免下次还比较这两个人
	comparePlayers = append(comparePlayers, []int{p1.chairID, p2.chairID})

	specialCard1 := p1.hcontext.cardDuns[0]
	specialCard2 := p2.hcontext.cardDuns[0]
	//先比较特殊牌型
	isSpecialCard := comparePlayerScoreDun(0, specialCard1, specialCard2, p1, p2)
	if !isSpecialCard {
		//都没特殊牌型 比较墩
		for i := 1; i < 4; i++ {
			dun := int32(i)
			sc1 := p1.hcontext.cardDuns[dun]
			sc2 := p2.hcontext.cardDuns[dun]
			comparePlayerScoreDun(dun, sc1, sc2, p1, p2)
		}
	}
}

//保存墩的分数详情(特殊牌型也在这保存)
func saveScoreDunInfo(dun int32, winPlayer *Player, losePlayer *Player) {
	winPlayerDun := winPlayer.hcontext.cardDuns[dun]
	losePlayerDun := losePlayer.hcontext.cardDuns[dun]

	//计算当前分数
	score := scoreOfCardType[xproto.CardHandType(winPlayerDun.GetCardType())]
	ws := winPlayerDun.GetScore()
	wScore := score + ws
	winPlayerDun.Score = &wScore

	ls := losePlayerDun.GetScore()
	lScore := ls - score
	losePlayerDun.Score = &lScore
	//把关系添加进去
	lChairs := winPlayerDun.GetLoserChairID()
	if lChairs == nil {
		lChairs = []int32{int32(losePlayer.chairID)}
	} else {
		lChairs = append(lChairs, int32(losePlayer.chairID))
	}
	winPlayerDun.LoserChairID = lChairs
}

func caleAndSaveScoreDun(dun int32, cards []int32, p *Player) {
	cardHand := patternConvertMsgCardHand(cards, p.cl)
	sd := &xproto.MsgPlayerScoreDun{}
	sd.CardType = cardHand.CardHandType
	sd.CardNum = &cardHand.Cards[0] //此处要保证最前面的牌是最大的
	sd.Dun = &dun

	p.hcontext.cardDuns[dun] = sd
}
