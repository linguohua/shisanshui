package tables

import (
	"shisanshui/xproto"
)

// onMessageDiscardHandler 响应玩家打牌动作，对于十三水来说就是玩家整理牌型完成，服务器比较玩家之间
// 的牌型大小，得出每个玩家赢取的分数
func onMessageDiscardHandler(s *statePlaying, p *Player, msg *xproto.MsgPlayerAction) {
	// 检查是否拥有这些牌
	cardsNum := make([]int, 52)
	cards := p.cards.hand2IDList(true)
	for _, card := range cards {
		cardsNum[card]++
	}
	for _, card := range msg.Cards {
		cardsNum[card]--
		if cardsNum[card] < 0 {
			//找不到某个牌
			s.cl.Panicf("onMessageDiscardHandler, player %s has no cardID %d in hand", p.ID, card)
			return
		}
	}

	s.waiter.takeAction(p, int(xproto.ActionType_enumActionType_DISCARD), msg.Cards)

	// 发送通知给所有客户端
	var msgActionNotifyResult = serializeMsgActionResultNotifyForNoTile(int(xproto.ActionType_enumActionType_DISCARD), p)

	// 发送结果给所有其他用户
	for _, pP := range s.table.players {
		if pP != p {
			pP.sendActionResultNotify(msgActionNotifyResult)
		}
	}
	//TODO 发给自己的 需要加上牌详情 客户端用于显示

	//计算牌型
	calcFinalResult(s, p, msg.Cards)
}
