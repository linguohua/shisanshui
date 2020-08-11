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
	//特殊牌型 (有特殊牌型的时候有效)
	specialCardType int32
	//特殊牌型牌大小 (有特殊牌型的时候有效，用于跟同样牌型比较大小)
	specialCardNum int32
	//特殊牌型赢了谁 (有特殊牌型的时候有效)
	loserChairID []int32
	//墩牌详情
	cardDuns []*xproto.MsgPlayerScoreDun
}

// pgameContext context for whold game
// reset when player exit
type pgameContext struct {
}
