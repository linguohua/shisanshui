package tables

import (
	"math/rand"
	"shisanshui/xproto"

	"github.com/sirupsen/logrus"
)

const (
	drawCountEach = 13
)

// dealer 发牌员，发牌给所有玩家
type dealer struct {
	cl *logrus.Entry

	table   *Table
	players []*Player

	wallCards  []*card
	customDraw []int

	rand *rand.Rand
}

func dealerNew(t *Table, players []*Player) *dealer {
	cl := t.cl.WithField("src", "dealer")

	d := &dealer{
		cl:      cl,
		players: players,
		table:   t,
		rand:    t.rand,
	}

	// no joker, 54 - 2 = 52
	maxCardCount := 52

	var wallCards = make([]*card, maxCardCount)
	cnt := 0
	for i := xproto.CardID_R2H; i < xproto.CardID_JOB; i++ {
		wallCards[cnt] = &card{cardID: i}
		cnt++
	}

	d.wallCards = shuffleArray(wallCards[0:cnt], d.rand)

	return d
}

func (d *dealer) drawForMonkeys() {
	cfg := d.table.monkeyCfg
	// 按照顺序为其他玩家抽牌
	for i, player := range d.players {
		tcfg := cfg.monkeyCardsCfgList[i]
		d.drawByMonkeyCardsCfg(player, tcfg)
	}

	// 为不足够牌的玩家补牌
	for _, player := range d.players {
		d.padPlayerCards(player)
	}
}

// padPlayerCards 如果玩家的手牌不足够13张则为其抽牌补足
func (d *dealer) padPlayerCards(p *Player) {
	var total = drawCountEach

	var reamin = total - p.cards.cardCountInHand()
	for i := 0; i < reamin; i++ {
		d.drawOne(p, false)
	}
}

// drawByMonkeyCardsCfg 为player填充手牌列表
func (d *dealer) drawByMonkeyCardsCfg(p *Player, cfgUserCards *monkeyCardConfig) {
	var cards = p.cards

	if len(cfgUserCards.handCards) > 0 {
		for _, cardID := range cfgUserCards.handCards {
			var t = d.extractWith(cardID)
			nt := &card{drawBy: p.ID, cardID: t.cardID}

			cards.addHandCard(nt)
		}
	}
}

// drawWith 从牌墙中抽取指定的牌
func (d *dealer) extractWith(cardID int) *card {
	var card = d.removeCardFromWall(cardID)

	if nil == card {
		d.cl.Panic("DrawWith, no card remain")
		return nil
	}

	return card
}

func (d *dealer) removeCardFromWall(cardID int) *card {
	for i, v := range d.wallCards {
		if v.cardID == xproto.CardID(cardID) {
			// 删除一个元素
			wt := d.wallCards[0:i]
			rm := d.wallCards[i+1:]
			d.wallCards = append(wt, rm...)

			return v
		}
	}

	return nil
}

func (d *dealer) extractOne() *card {
	// monkey测试如果配置了抽牌序列则按照配置来抽牌
	if len(d.customDraw) > 0 {
		cardID := d.customDraw[0]
		d.customDraw = d.customDraw[1:]
		t := d.removeCardFromWall(cardID)

		//Debug.Assert(t != null, "custom draw failed")
		if t == nil {
			d.cl.Println("custom draw failed:", cardID)
		} else {
			return t
		}
	}

	if len(d.wallCards) < 1 {
		d.cl.Panic("wallCards is empty")
		return nil
	}

	t := d.wallCards[0]
	// 如果此时wallCards为1长度，[1:]则使得新数组长度为0
	d.wallCards = d.wallCards[1:]

	return t
}

func (d *dealer) extractOneReverse() *card {
	// TODO:
	return nil
}

// drawOne draw one card for player
func (d *dealer) drawOne(p *Player, reverse bool) (ok bool, handCard *card) {
	handCard = nil

	if len(d.wallCards) < 1 {
		d.cl.Panic("wall cards empty")
		ok = false
		return
	}

	ok = false

	for len(d.wallCards) > 0 {
		var t *card
		if !reverse {
			t = d.extractOne()
		} else {
			t = d.extractOneReverse()
		}

		newCard := &card{drawBy: p.ID, cardID: t.cardID}

		// 普通牌，停止抽牌
		p.cards.addHandCard(newCard)
		handCard = newCard
		ok = true
		break
		// }
	}

	return
}

// drawForAll 为所有人发牌
func (d *dealer) drawForAll() {
	for _, player := range d.players {
		if !player.cards.isEmpty() {
			d.cl.Panic("Player card list should be empty")
			player.cards.clear()
		}
	}

	if d.table.monkeyCfg != nil {
		d.drawForMonkeys()
		return
	}

	// 抽取drawCountEach张牌
	for i := 0; i < drawCountEach; i++ {
		for _, player := range d.players {
			// 不会出现无牌可抽情况
			d.drawOne(player, false)
		}
	}
}
