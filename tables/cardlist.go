package tables

import (
	"container/list"
	"shisanshui/xproto"

	"github.com/sirupsen/logrus"
)

type cardList = *list.List

// cardlist 玩家的牌列表
type cardlist struct {
	cl     *logrus.Entry
	player *Player
	// 玩家手上的牌列表
	hand cardList
}

func cardlistNew(p *Player) *cardlist {
	cl := p.cl.WithField("src", "cardlist")

	cli := &cardlist{
		cl:     cl,
		player: p,
		hand:   list.New(),
	}

	return cli
}

func (cli *cardlist) isEmpty() bool {
	// TODO:
	return true
}

func (cli *cardlist) clear() {
	cli.cl.Println("clear")
}

func (cli *cardlist) addHandCard(newCard *card) {
	// TODO:
}

// hand2IDList 手牌列表序列化到ID list，用于消息发送
func (cli *cardlist) hand2IDList(isShowDarkCards bool) []int32 {
	int32List := make([]int32, cli.hand.Len())
	var i = 0
	for e := cli.hand.Front(); e != nil; e = e.Next() {
		if isShowDarkCards {
			int32List[i] = int32(e.Value.(*card).cardID)
		} else {
			//不显示牌的话 牌要盖着
			int32List[i] = int32(xproto.CardID_CARDMAX)
		}
		i++
	}
	return int32List
}

// cardCountInHand 手牌的数量
func (cli *cardlist) cardCountInHand() int {
	return cli.hand.Len()
}
