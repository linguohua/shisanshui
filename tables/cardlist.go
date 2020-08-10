package tables

import (
	"container/list"
	"shisanshui/xproto"

	"github.com/sirupsen/logrus"
)

// cardlist 玩家的牌列表
type cardlist struct {
	cl     *logrus.Entry
	player *Player

	cardsInHand *list.List
}

func cardlistNew(p *Player) *cardlist {
	cl := p.cl.WithField("src", "cardlist")

	cli := &cardlist{
		cl:          cl,
		player:      p,
		cardsInHand: list.New(),
	}

	return cli
}

func (cli *cardlist) isEmpty() bool {
	return cli.cardsInHand.Len() < 1
}

func (cli *cardlist) clear() {
	cli.cl.Println("clear")
	cli.cardsInHand.Init()
}

func (cli *cardlist) cardCountInHand() int {
	return cli.cardsInHand.Len()
}

func (cli *cardlist) addHandCard(newCard *card) {
	if cli.cardCountInHand() >= 13 {
		cli.cl.Panic("Total cards must less than 21")
		return
	}

	cli.cardsInHand.PushBack(newCard)
}

// hand2IDList 手牌列表序列化到ID list，用于消息发送
func (cli *cardlist) hand2IDList(isShowDarkCards bool) []int32 {
	int32List := make([]int32, cli.cardsInHand.Len())
	var i = 0
	for e := cli.cardsInHand.Front(); e != nil; e = e.Next() {
		if isShowDarkCards {
			int32List[i] = int32(e.Value.(*card).cardID)
		} else {
			//不显示牌的话  要盖着
			int32List[i] = int32(xproto.CardID_CARDMAX)
		}
		i++
	}
	return int32List
}
