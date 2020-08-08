package tables

import (
	"shisanshui/xproto"
)

// onMessageDiscardHandler 响应玩家打牌动作，对于十三水来说就是玩家整理牌型完成，服务器比较玩家之间
// 的牌型大小，得出每个玩家赢取的分数
func onMessageDiscardHandler(s *statePlaying, p *Player, msg *xproto.MsgPlayerAction) {
	// TODO:
}
