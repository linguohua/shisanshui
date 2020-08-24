// 消息打包
// 打包发送给客户端的消息

package tables

import (
	"shisanshui/xproto"
)

func serializeMsgRestore(s *statePlaying, player *Player) *xproto.MsgRestore {
	//序列化掉线恢复消息给客户端
	msgRestore := &xproto.MsgRestore{}

	msgDeal := serializeMsgDeal(s, player)
	msgRestore.MsgDeal = msgDeal
	//TODO ：如果已经结算，另外处理

	return msgRestore
}

func serializeMsgDeal(s *statePlaying, my *Player) *xproto.MsgDeal {
	// 发牌消息
	var msg = &xproto.MsgDeal{}
	playerCardLists := make([]*xproto.MsgPlayerCardList, len(s.playingPlayers))

	for i, p := range s.playingPlayers {
		var cardList *xproto.MsgPlayerCardList
		if p == my {
			cardList = serializeCardList(p, true)
		} else {
			cardList = serializeCardList(p, false)
		}
		playerCardLists[i] = cardList
	}

	msg.PlayerCardLists = playerCardLists
	bankerChairID := int32(s.table.bankerChairID)
	msg.BankerChairID = &bankerChairID

	return msg
}

func serializeMsgAllowedForDiscard(s *statePlaying, p *Player, actions int, qaIndex int, timeout int32) *xproto.MsgAllowAction {
	// 序列化某个玩家出牌是允许的动作
	var msg = &xproto.MsgAllowAction{}
	var qaIndex32 = int32(qaIndex)
	msg.QaIndex = &qaIndex32
	var allowedActions32 = int32(actions)
	msg.AllowedActions = &allowedActions32
	var chairID32 = int32(p.chairID)
	msg.ActionChairID = &chairID32
	var timeout32 = int32(timeout)
	msg.TimeoutInSeconds = &timeout32

	return msg
}

func serializeCardList(player *Player, isShowDarkCards bool) *xproto.MsgPlayerCardList {
	// 序列化牌列表给自己
	playerCardList := &xproto.MsgPlayerCardList{}
	cards := player.cards
	var chairID = int32(player.chairID)
	playerCardList.ChairID = &chairID
	// 手牌
	var cardCountInHand = int32(cards.cardCountInHand())
	playerCardList.CardCountOnHand = &cardCountInHand
	playerCardList.CardsOnHand = cards.hand2IDList(isShowDarkCards)
	return playerCardList
}

func serializeMsgTableInfo(t *Table, timeout int32) *xproto.MsgTableInfo {
	// serializeMsgTableInfo 序列化房间信息给客户端
	msg := &xproto.MsgTableInfo{}

	var state32 = int32(t.state.getStateConst())
	msg.State = &state32
	var tableNumber = t.Number
	msg.TableNumber = &tableNumber

	msg.Timeout = &timeout
	// var handStartted32 = int32(r.handRoundStarted)
	// msg.HandStartted = &handStartted32
	// var handFinished32 = int32(r.handRoundFinished)
	// msg.HandFinished = &handFinished32

	playerInfos := make([]*xproto.MsgPlayerInfo, len(t.players))
	for i, p := range t.players {
		var msgPlayerInfo = &xproto.MsgPlayerInfo{}
		var chairID32 = int32(p.chairID)
		msgPlayerInfo.ChairID = &chairID32
		var userID = p.ID
		msgPlayerInfo.UserID = &userID
		var pstate32 = int32(p.state)
		msgPlayerInfo.State = &pstate32

		// var userInfo = p.user.getInfo()
		// msgPlayerInfo.Nick = &userInfo.nick
		// msgPlayerInfo.Sex = &userInfo.sex
		// msgPlayerInfo.HeadIconURI = &userInfo.headIconURI
		// msgPlayerInfo.Ip = &userInfo.ip
		// msgPlayerInfo.Location = &userInfo.location
		// var dfHands = int32(userInfo.dfHands)
		// msgPlayerInfo.DfHands = &dfHands

		// var diamond32 = int32(userInfo.diamond)
		// msgPlayerInfo.Diamond = &diamond32
		// var charm32 = int32(userInfo.charm)
		// msgPlayerInfo.Charm = &charm32
		// var avatarID = int32(userInfo.avatarID)
		// msgPlayerInfo.AvatarID = &avatarID
		// var clubIDs = userInfo.clubIDs
		// msgPlayerInfo.ClubIDs = clubIDs
		// var dan = int32(userInfo.dan)
		// msgPlayerInfo.Dan = &dan
		// var isLooker = bool(p.isLooker)
		// msgPlayerInfo.IsLooker = &isLooker
		playerInfos[i] = msgPlayerInfo
	}

	msg.Players = playerInfos
	// msg.ScoreRecords = r.scoreRecords

	return msg
}

func serializeMsgHandOver(s *statePlaying) *xproto.MsgHandOver {
	// serializeMsgHandOver 序列化单手牌结束的消息给客户端
	// 计算分数
	var msgHandScore = &xproto.MsgHandScore{}

	playerScores := make([]*xproto.MsgPlayerScore, 0, len(s.playingPlayers))

	for _, player := range s.playingPlayers {
		var msgPlayerScore = &xproto.MsgPlayerScore{}
		rContext := player.rContext
		msgPlayerScore.TotalScore = &rContext.totalScore
		chairid := int32(player.chairID)
		msgPlayerScore.TargetChairID = &chairid
		msgPlayerScore.SpecialCardHand = rContext.specialCardHand
		msgPlayerScore.IsWinAll = &rContext.isWinAll
		msgPlayerScore.IsInvertedHand = &rContext.isInvertedHand
		//与其他玩家关系
		compareContexts := make([]*xproto.MsgPlayerCompareContext, 0, 3)
		for _, cC := range rContext.compareContexts {
			compareContext := &xproto.MsgPlayerCompareContext{}
			tc := int32(cC.target.chairID)
			compareContext.TargetChairID = &tc
			compareContext.TotalScore = &cC.compareTotalScore
			compareContext.LoseHandNum = &cC.loseHandNum
			compareContext.HandTotalScore = cC.handTotalScore

			compareContexts = append(compareContexts, compareContext)
		}
		msgPlayerScore.CompareContexts = compareContexts
		playerScores = append(playerScores, msgPlayerScore)
	}

	msgHandScore.PlayerScores = playerScores

	// 构造MsgHandOver
	msgHandOver := &xproto.MsgHandOver{}
	msgHandOver.Scores = msgHandScore
	endType := int32(xproto.HandOverType_enumHandOverType_None)
	msgHandOver.EndType = &endType
	msgHandOver.PlayerCardLists = serializeCardListsForHandOver(s)

	s.cl.Println("serializeMsgHandOver data : ", msgHandOver)
	return msgHandOver
}

// serializeCardListsForHandOver 序列化单手牌结束消息给客户端
func serializeCardListsForHandOver(s *statePlaying) []*xproto.MsgPlayerCardList {
	playerCardLists := make([]*xproto.MsgPlayerCardList, len(s.playingPlayers))

	for i, p := range s.playingPlayers {
		var cardList *xproto.MsgPlayerCardList
		cardList = serializeCardList(p, true)
		playerCardLists[i] = cardList
	}

	return playerCardLists
}

func serializeMsgActionResultNotifyForAll(actoin int, player *Player) *xproto.MsgActionResultNotify {
	// 序列化某个玩家的动作结果给其他玩家
	var msg = &xproto.MsgActionResultNotify{}
	var action32 = int32(actoin)
	msg.Action = &action32
	var chairID32 = int32(player.chairID)
	msg.TargetChairID = &chairID32

	return msg
}

func serializeMsgActionResultNotifyForSelfDiscard(actoin int, player *Player) *xproto.MsgActionResultNotify {
	// 序列化自己出牌结果给自己
	var msg = &xproto.MsgActionResultNotify{}
	var action32 = int32(actoin)
	msg.Action = &action32
	var chairID32 = int32(player.chairID)
	msg.TargetChairID = &chairID32
	//特殊牌型排序 可能为nil
	if player.rContext.specialCardHand != nil {
		msg.ActionHands = []*xproto.MsgCardHand{player.rContext.specialCardHand}
	} else {
		msg.ActionHands = player.rContext.hands
	}

	return msg
}
