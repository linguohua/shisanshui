package tables

import (
	"shisanshui/xproto"
)

// card 一张牌
type card struct {
	// 花色点数
	cardID xproto.CardID
	// 抽取者ID
	drawBy string
}

// hand 牌型
type hand struct {
	// 列表
	cards []*card
	// 牌型类型，例如同花，顺子等
	htype xproto.CardHandType
}
