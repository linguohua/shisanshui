package tables

import (
	"shisanshui/xproto"
)

// onMessageDiscardHandler 响应玩家打牌动作，对于十三水来说就是玩家整理牌型完成，服务器比较玩家之间
// 的牌型大小，得出每个玩家赢取的分数
func onMessageDiscardHandler(s *statePlaying, p *Player, msg *xproto.MsgPlayerAction) {
	// TODO:检查是否拥有这些牌

	s.waiter.takeAction(p, int(xproto.ActionType_enumActionType_DISCARD), msg.Cards)

	// 发送通知给所有客户端
	var msgActionNotifyResult = serializeMsgActionResultNotifyForNoTile(int(xproto.ActionType_enumActionType_DISCARD), p)

	// 发送结果给所有其他用户
	for _, pP := range s.table.players {
		// if pP != p {
		pP.sendActionResultNotify(msgActionNotifyResult)
		// }
	}
}
