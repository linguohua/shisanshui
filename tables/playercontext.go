package tables

import (
	"shisanshui/xproto"
)

// phandContext context for one hand card
// reset when one hand completed
// 每一手牌开始时重新生成
type phandContext struct {
	expectedAction int
	//理牌结果
	sortCards []int32
	//墩牌详情 (0号元素 记录特殊牌型)
	cardDuns map[int32]*xproto.MsgPlayerScoreDun
}

// pgameContext context for whold game
// reset when player exit
type pgameContext struct {
}
