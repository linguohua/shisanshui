// 计算每一个玩家的输赢得分

package tables

import "shisanshui/xproto"

func calcFinalResult(s *statePlaying) {
	//计算结果
	for _, p := range s.playingPlayers {
		//TODO ：判断是否 倒墩(弃权) 是的话就不参与比较
		cardHand := patternConvertMsgCardHand(p.hcontext.sortCards, p.cl)
		p.hcontext.specialCardType = *cardHand.CardHandType
		if *cardHand.CardHandType != int32(xproto.CardHandType_None) {
			//有特殊牌型
			p.hcontext.specialCardNum = cardHand.Cards[0] //此处要保证最前面的牌是最大的
		} else {
			//没有特殊牌型 要每一墩都计算
			if len(p.hcontext.sortCards) == 13 {
				p.hcontext.cardDuns = make([]*xproto.MsgPlayerScoreDun, 3)

				dun1 := p.hcontext.sortCards[0:5]
				dun2 := p.hcontext.sortCards[5:10]
				dun3 := p.hcontext.sortCards[10:13]
				caleAndSaveScoreDun(1, dun1, p)
				caleAndSaveScoreDun(2, dun2, p)
				caleAndSaveScoreDun(3, dun3, p)
			}
		}
	}
	//比较大小
	for _, myP := range s.playingPlayers {
		for _, otherP := range s.playingPlayers {
			//我自己 跟其他人比较 输赢只记录我自己的就可以
			if myP != otherP {
				if myP.hcontext.specialCardType != int32(xproto.CardHandType_None) {
					//先比较特殊牌型
				} else {
					//再比较墩
				}
			}
		}
	}

}

func caleAndSaveScoreDun(dun int32, cards []int32, p *Player) {
	cardHand := patternConvertMsgCardHand(cards, p.cl)
	sd := &xproto.MsgPlayerScoreDun{}
	sd.CardType = cardHand.CardHandType
	sd.CardNum = &cardHand.Cards[0] //此处要保证最前面的牌是最大的
	sd.Dun = &dun

	p.hcontext.cardDuns[dun-1] = sd
}
