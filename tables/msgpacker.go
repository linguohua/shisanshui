// 消息打包
// 打包发送给客户端的消息

package tables

import (
	"shisanshui/xproto"
)

func serializeMsgDeal(s *statePlaying, my *Player, players []*Player) *xproto.MsgDeal {
	// TODO:发牌消息
	var msg = &xproto.MsgDeal{}
	playerCardLists := make([]*xproto.MsgPlayerCardList, len(players))

	for i, p := range players {
		var cardList *xproto.MsgPlayerCardList
		if p == my {
			cardList = serializeCardList(p, true)
		} else {
			cardList = serializeCardList(p, false)
		}
		playerCardLists[i] = cardList
	}

	msg.PlayerCardLists = playerCardLists

	return msg
}

func serializeMsgAllowedForDiscard(s *statePlaying, p *Player, actions int, qaIndex int, betNumber int32) *xproto.MsgAllowAction {
	// TODO:序列化某个玩家出牌是允许的动作
	var msg = &xproto.MsgAllowAction{}
	var qaIndex32 = int32(qaIndex)
	msg.QaIndex = &qaIndex32
	var allowedActions32 = int32(actions)
	msg.AllowedActions = &allowedActions32
	var chairID32 = int32(p.chairID)
	msg.ActionChairID = &chairID32
	var timeout32 = int32(betNumber)
	msg.TimeoutInSeconds = &timeout32

	return msg
}

// serializeCardList 序列化牌列表给自己
func serializeCardList(player *Player, isShowDarkCards bool) *xproto.MsgPlayerCardList {
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
