// 计算每一个玩家的输赢得分

package tables

import "shisanshui/xproto"

var (
	//非以下牌型：每墩赢了会获得1分。
	//第一墩四条：如果玩家第一墩有1组四条而获得胜利，那会获得4分（水）
	//第一墩同花顺：如果玩家第一墩有1组同花顺而获得胜利，那会获得5分（水）
	//第二墩四条：如果玩家第二墩有1组四条而获得胜利，那会获得8分（水）
	//第二墩同花顺：如果玩家第二墩有1组同花顺而获得胜利，那会获得10分（水）
	//第二墩葫芦：如果玩家第二墩有1组葫芦而获得胜利，那会获得2分（水）
	//第三墩三条：如果玩家第3墩有1组三条而获得胜利，那会获得3分（水）
	//墩牌型对应得分
	scoreOfHandType = []map[xproto.CardHandType]int32{
		{
			// 同花顺	Straight Flush 五张或更多的连续单牌（如： 45678 或 78910JQK ）
			xproto.CardHandType_StraightFlush: 5,
			// 四条 Four of a Kind：四张同点牌 + 一张
			xproto.CardHandType_Four: 4,
		},
		{
			// 同花顺	Straight Flush 五张或更多的连续单牌（如： 45678 或 78910JQK ）
			xproto.CardHandType_StraightFlush: 10,
			// 四条 Four of a Kind：四张同点牌 + 一张
			xproto.CardHandType_Four: 8,
			// 葫芦
			xproto.CardHandType_FullHouse: 2,
		},
		{
			// 三条 Three of a kind
			xproto.CardHandType_ThreeOfAKind: 3,
		},
	}
	scoreOfSpecialType = map[xproto.SpecialType]int32{
		//特殊牌型
		// 三顺子 每一墩都是顺子（如  23456、45678、789）
		xproto.SpecialType_Three_Straight: 9,
		// 三同花 每一墩花色相同（如 第一墩都是梅花 第二墩都是方块）
		xproto.SpecialType_Three_Flush: 9,
		// 六对半
		xproto.SpecialType_SixPairs_HighCard: 9,
		// 五对 加 三条
		xproto.SpecialType_FivePairs_ThreeOfAKind: 18,
		// 一点黑
		xproto.SpecialType_One_Black: 24,
		// 一点红
		xproto.SpecialType_One_Red: 24,
		// 清一色 (全黑或全红 可以方块红桃混合)
		xproto.SpecialType_Pure_One_Suit: 30,
		// 一条龙
		xproto.SpecialType_All_Straight: 39,
		// 至尊清龙
		xproto.SpecialType_All_StraightFlush: 78,
	}
)

// 客户端发送结果上来,就计算牌型结果(牌型,倒墩)
// 比较：先看看是不是特殊牌型 不是的话再各个墩比较 并且看看是不是三墩皆输
// 结算的时候 看看有没三家皆赢 再计算分数

//客户端结果上来后调用 计算结果入口
func calcFinalResult(s *statePlaying, p *Player, cards []int32) {
	//计算结果
	//判断是否是特殊牌型 (判断函数还没写)
	cardHand := calc13(cards, p.cl)
	if cardHand.GetCardHandType() != int32(xproto.SpecialType_Special_None) {
		//有特殊牌型
		//TODO 如果有特殊牌型 三顺子 三同花 (是不是也要判断倒墩)
		p.rContext.specialCardHand = cardHand
	} else {
		p.rContext.hands = make([]*xproto.MsgCardHand, 3)
		//没有特殊牌型 要每一墩都计算
		hand1 := cards[0:5]
		hand2 := cards[5:10]
		hand3 := cards[10:13]
		cardT1 := caleAndSaveHand(0, hand1, p)
		cardT2 := caleAndSaveHand(1, hand2, p)
		cardT3 := caleAndSaveHand(2, hand3, p)
		// 判断是否倒墩
		p.rContext.isInvertedHand = isInvertedHand(cardT1, cardT2, cardT3)
	}
	//创建与其他玩家分数关系列表
	p.rContext.compareContexts = make([]*compareContext, 3)
}

//特殊牌型是不是倒墩
func isInvertedHandWithSpecial(cardHand *xproto.MsgCardHand) bool {
	//如果有特殊牌型 三顺子 三同花
	// if cardType == xproto.SpecialType_Three_Flush {

	// }
	// if cardType == xproto.SpecialType_Three_Straight {

	// }
	return false
}

//判断是否是倒墩
func isInvertedHand(hand1, hand2, hand3 *xproto.MsgCardHand) bool {
	//先比较 1.2墩
	if hand1.GetCardHandType() < hand2.GetCardHandType() {
		return true
	}
	if hand1.GetCardHandType() == hand2.GetCardHandType() {
		r := getCardsCompareResult(hand1.GetCards(), hand2.GetCards())
		if r == 2 {
			return true
		}
	}
	//比较 第三墩
	if hand3.GetCardHandType() < int32(xproto.CardHandType_Straight) {
		//三条 对子 单张 (如果第三墩比其他两墩大 都是倒墩)
		if hand3.GetCardHandType() == hand1.GetCardHandType() {
			r := getCardsCompareResult(hand1.GetCards(), hand3.GetCards())
			if r == 2 {
				return true
			}
		}
		if hand3.GetCardHandType() == hand2.GetCardHandType() {
			r := getCardsCompareResult(hand2.GetCards(), hand3.GetCards())
			if r == 2 {
				return true
			}
		}
		if hand1.GetCardHandType() < hand3.GetCardHandType() ||
			hand2.GetCardHandType() < hand3.GetCardHandType() {
			return true
		}
	} else {
		if compareHandAndReturnResult(hand1, hand3) {
			return true
		}
		if compareHandAndReturnResult(hand2, hand3) {
			return true
		}
	}
	return false
}

//第三墩 跟 第一二墩比较 (返回true 说明倒墩)
func compareHandAndReturnResult(hand, hand3 *xproto.MsgCardHand) bool {
	// 第三墩是顺子 并且 跟另外两墩相连 则第三墩应该是最小的那三张
	// 第三墩是同花 并且 跟另外两墩花色相同 则第三墩应该是最小的三张
	if hand3.GetCardHandType() == hand.GetCardHandType() {
		if hand3.GetCardHandType() == int32(xproto.CardHandType_Flush) {
			if hand3.Cards[0]%4 == hand.Cards[0]%4 {
				return isBigOfCardsfigure(hand.GetCards(), hand3.GetCards(), false)
			}
		}
		if hand3.GetCardHandType() == int32(xproto.CardHandType_Straight) {
			hai := append(hand3.GetCards(), hand.GetCards()...)
			if patternVerifyStraight(hai) {
				//两墩牌合起来如果是顺子 说明是相连的
				return isBigOfCardsfigure(hand.GetCards(), hand3.GetCards(), true)
			}
		}
	}
	return false
}

//第三墩 是不是有的牌大于 第一二墩的最小牌 (返回true 说明倒墩)
func isBigOfCardsfigure(hand, handThree []int32, isStraight bool) bool {
	//最小牌
	smallCard := hand[4]
	if isStraight {
		//顺子要考虑A
		if hand[0]/4 == int32(xproto.CardID_AC)/4 && hand[len(hand)-1]/4 == int32(xproto.CardID_R2C)/4 {
			// A, 5, ...
			smallCard = hand[len(hand)-1]
		}
		if handThree[0]/4 == int32(xproto.CardID_AC)/4 && handThree[len(handThree)-1]/4 == int32(xproto.CardID_R2C)/4 {
			handThree = handThree[1:]
		}
	}
	for _, v := range handThree {
		if v > smallCard {
			return true
		}
	}

	return false
}

//计算墩的牌型 并保存结果到 player.rContext
func caleAndSaveHand(hand int32, cards []int32, p *Player) *xproto.MsgCardHand {
	cardHand := patternConvertMsgCardHand(cards, p.cl)
	p.rContext.hands[hand] = cardHand

	return cardHand
}

//两两比较结果
func comparePlayerResult(p1 *Player, p2 *Player) {
	//创建彼此比较对象
	compareContext2 := &compareContext{}
	p1.rContext.compareContexts[p2.chairID] = compareContext2
	compareContext1 := &compareContext{}
	p2.rContext.compareContexts[p1.chairID] = compareContext1
	//先比较特殊牌型
	haveSpecialCard := haveSpecialCardTypeAndSaveScore(p1, p2)
	if !haveSpecialCard {
		//都没特殊牌型 比较墩
		for i := 0; i < 3; i++ {
			hand := int32(i)
			sc1 := p1.rContext.hands[hand]
			sc2 := p2.rContext.hands[hand]
			compareHandAndSaveScore(hand, sc1, sc2, p1, p2)
		}
	}
}

//计算墩的分数
func calcHandScore(hand int32, handType xproto.CardHandType) int32 {
	// 没有牌型就1分
	score := int32(1)
	if handType != xproto.CardHandType_CardHand_None {
		score = scoreOfHandType[hand][handType]
		if score > 0 {
			return score
		}
	}

	return score
}

//比较墩 并 保存基础分数
func compareHandAndSaveScore(hand int32, ps1, ps2 *xproto.MsgCardHand, p1, p2 *Player) {
	score := int32(0)
	winer := p1
	loser := p2
	//先看看是不是倒墩
	if p1.rContext.isInvertedHand && p2.rContext.isInvertedHand {
		return
	}
	if p1.rContext.isInvertedHand {
		winer = p2
		loser = p1
	} else if !p1.rContext.isInvertedHand && !p2.rContext.isInvertedHand {
		if ps2.GetCardHandType() > ps1.GetCardHandType() {
			winer = p2
			loser = p1
		} else if ps1.GetCardHandType() == ps2.GetCardHandType() {
			//比较同种牌型大小 用最大牌点数比较 相同就往下一张...
			r := getCardsCompareResult(ps1.GetCards(), ps2.GetCards())
			if r == 2 {
				winer = p2
				loser = p1
			}
			if r == 0 {
				return
			}
		}
	}
	score = calcHandScore(hand, xproto.CardHandType(winer.rContext.hands[hand].GetCardHandType()))
	//赢的一方 添加到输的一方的compareContexts列表里
	winCompareContext := loser.rContext.compareContexts[winer.chairID]
	winCompareContext.handTotalScore[hand] = score
	winCompareContext.compareTotalScore += score
	winer.rContext.totalScore += score
	//输的一方 添加到赢的一方的compareContexts列表里
	loseCompareContext := winer.rContext.compareContexts[loser.chairID]
	loseCompareContext.handTotalScore[hand] = -score
	loseCompareContext.loseHandNum++
	loseCompareContext.compareTotalScore -= score
	loser.rContext.totalScore -= score
}

//比较牌组点数大小 返回 0：相等 1：第一个参数大  2：第二个参数大
func getCardsCompareResult(cards1, cards2 []int32) int {
	//用短的长度
	l1 := len(cards1)
	l2 := len(cards2)
	if l1 > l2 {
		l1 = l2
	}
	//比较同种牌型大小 用最大牌点数比较 相同就往下一张...
	for i := 0; i < l1; i++ {
		if cards1[i] > cards2[i] {
			return 1
		}
		if cards2[i] > cards1[i] {
			return 2
		}
	}
	return 0
}

//看看是否有特殊牌型 并保存基础分
func haveSpecialCardTypeAndSaveScore(p1, p2 *Player) bool {
	pContext1 := p1.rContext
	pContext2 := p2.rContext
	if pContext1.specialCardHand.GetCardHandType() == int32(xproto.SpecialType_Special_None) &&
		pContext2.specialCardHand.GetCardHandType() == int32(xproto.SpecialType_Special_None) {
		return false
	}
	score := int32(0)
	winer := p1
	loser := p2
	if pContext2.specialCardHand.GetCardHandType() > pContext1.specialCardHand.GetCardHandType() {
		winer = p2
		loser = p1
	} else if pContext1.specialCardHand.GetCardHandType() == pContext2.specialCardHand.GetCardHandType() {
		//比较同种牌型大小 用最大牌点数比较 相同就往下一张...
		r := getCardsCompareResult(pContext1.specialCardHand.GetCards(), pContext2.specialCardHand.GetCards())
		if r == 2 {
			winer = p2
			loser = p1
		}
		if r == 0 {
			return true
		}
	}
	score = scoreOfSpecialType[xproto.SpecialType(winer.rContext.specialCardHand.GetCardHandType())]
	winCompareContext := loser.rContext.compareContexts[winer.chairID]
	winCompareContext.target = winer
	winCompareContext.compareTotalScore += score
	winer.rContext.totalScore += score

	loseCompareContext := winer.rContext.compareContexts[loser.chairID]
	loseCompareContext.target = loser
	loseCompareContext.compareTotalScore -= score
	loser.rContext.totalScore -= score

	return true
}

//比较大小 算分
func compareAndCalcScore(s *statePlaying) {
	//按chairid排序player 便于比较
	sortPlayers := make([]*Player, 4)
	for _, p := range s.playingPlayers {
		sortPlayers[p.chairID] = p
	}
	playerLen := len(sortPlayers)
	//比较大小
	for i := 0; i < playerLen-1; i++ {
		for j := i + 1; j < playerLen; j++ {
			p1 := sortPlayers[i]
			p2 := sortPlayers[j]
			comparePlayerResult(p1, p2)
		}
	}
	//看看是不是三家皆赢
	for _, p := range s.playingPlayers {
		//非特殊牌型才看三家皆赢
		if p.rContext.specialCardHand.GetCardHandType() != int32(xproto.SpecialType_Special_None) {
			loserNum := 0
			for _, cC := range p.rContext.compareContexts {
				if cC.loseHandNum == 3 {
					loserNum++
				}
			}
			if loserNum == 3 {
				p.rContext.isWinAll = true
			}
		}
	}
	//计算最后分数 (看看有没有加倍)
	for _, p := range s.playingPlayers {
		calcAddedScore(p)
	}
}

//计算额外加分
func calcAddedScore(p *Player) {
	pr := p.rContext
	//墩分数
	//先看是不是三墩皆输 是的话 就*2
	//再看看是不是同时满足 倒墩 三家皆赢 是的话 再*2
	for _, cC := range pr.compareContexts {
		if cC.loseHandNum == 3 {
			targetP := cC.target
			for hand, score := range cC.handTotalScore {
				//需要修改的基数
				changeScore := score
				if targetP.rContext.isInvertedHand && pr.isWinAll {
					changeScore = changeScore * 3
				}
				//先修改p的
				cC.handTotalScore[hand] += changeScore
				cC.compareTotalScore += changeScore
				pr.totalScore += changeScore
				//再修改target的
				tcC := targetP.rContext.compareContexts[p.chairID]
				tcC.handTotalScore[hand] -= changeScore
				tcC.compareTotalScore -= changeScore
				targetP.rContext.totalScore -= changeScore
			}
		}
	}
}
