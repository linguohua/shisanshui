package tables

import (
	"shisanshui/xproto"
)

type card struct {
	cardID xproto.CardID
	drawBy string
}

type hand struct {
	cards []*card
	htype xproto.CardHandType
}
