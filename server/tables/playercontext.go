package tables

import (
	"shisanshui/xproto"
)

// phandContext context for one hand card
// reset when one hand completed
// 每一手牌开始时重新生成
type roundContext struct {
	expectedAction int
	//是否倒墩
	isInvertedHand bool
	//墩列表 0、1、2 表示 1、2、3墩
	hands []*xproto.MsgCardHand
	// 特殊牌型
	specialCardHand *xproto.MsgCardHand
	//与玩家分数关系
	compareContexts []*compareContext
	//是否三家皆赢
	isWinAll bool
	//总分
	totalScore int32
}

//与每个玩家分数关系
type compareContext struct {
	target            *Player
	compareTotalScore int32
	//记录玩家赢了target多少墩 (也就是target输的墩数)
	loseHandNum    int32
	handTotalScore []int32 //负分表示输家
}

// pgameContext context for whold game
// reset when player exit
type pgameContext struct {
}
