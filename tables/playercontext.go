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
	//墩列表
	hands []*xproto.MsgCardHand
	// 特殊牌型
	specialCardType int32
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
	loseHandNum         int32
	handCompareContexts []*handCompareContext
}

//每一墩的具体情况
type handCompareContext struct {
	handTotalScore int32
}

// pgameContext context for whold game
// reset when player exit
type pgameContext struct {
}
