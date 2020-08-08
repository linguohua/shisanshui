package tables

import "github.com/sirupsen/logrus"

type cardlist struct {
	cl     *logrus.Entry
	player *Player
}

func cardlistNew(p *Player) *cardlist {
	cl := p.cl.WithField("src", "cardlist")

	cli := &cardlist{
		cl:     cl,
		player: p,
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
