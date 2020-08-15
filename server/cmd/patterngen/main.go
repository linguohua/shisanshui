package main

import (
	"fmt"
	"shisanshui/xproto"
	"sort"

	log "github.com/sirupsen/logrus"
)

var (
	keyTags = make(map[int64]int)
)

func makeTagValue(haiCount int, ct xproto.CardHandType, flushCount int) int {
	// 第一个字节是类型
	// 第二个字节是牌张数
	// 第三个字节判断是否需要顺子检查
	v := int(ct)
	v |= (haiCount << 8)

	if flushCount > 0 {
		v |= (flushCount << 16)
	}

	return v
}

// 单牌：单个牌（如红桃 5 ），顺子，同花顺都复用该结构
func genSingleTag() {
	keyTags[11111] = makeTagValue(5, xproto.CardHandType_HighCard, 1)
}

// 四条
func genFourTag() {
	keyTags[41] = makeTagValue(5, xproto.CardHandType_Four, 0)
}

// 葫芦
func genFullHouseTag() {
	keyTags[32] = makeTagValue(5, xproto.CardHandType_FullHouse, 0)
}

// 三张牌：数值相同的三张牌（如三个 J ）
func genThreeOfAKindTag() {
	keyTags[311] = makeTagValue(5, xproto.CardHandType_ThreeOfAKind, 0)
}

// 两对牌
func genTwoPairsTag() {
	keyTags[221] = makeTagValue(5, xproto.CardHandType_TwoPairs, 0)
}

// 对牌
func genOnePairTag() {
	keyTags[2111] = makeTagValue(5, xproto.CardHandType_OnePair, 0)
}

// 单牌：单个牌（如红桃 5 ），顺子，同花顺都复用该结构
func gen3SingleTag() {
	keyTags[111] = makeTagValue(3, xproto.CardHandType_HighCard, 1)
}

// 三张牌：数值相同的三张牌（如三个 J ）
func gen3ThreeOfAKindTag() {
	keyTags[3] = makeTagValue(3, xproto.CardHandType_ThreeOfAKind, 0)
}

// 对牌
func gen3OnePairTag() {
	keyTags[21] = makeTagValue(3, xproto.CardHandType_OnePair, 0)
}

func genAllTag() {
	genSingleTag()
	genFourTag()
	genFullHouseTag()
	genThreeOfAKindTag()
	genTwoPairsTag()
	genOnePairTag()

	gen3SingleTag()
	gen3ThreeOfAKindTag()
	gen3OnePairTag()
}

func calcKeyTag(haiRank []int) int64 {
	// 最多14种牌
	slots := make([]int, 14)

	for _, r := range haiRank {
		slots[r]++
	}

	for _, s := range slots {
		if s > 4 {
			log.Panicln("slots elem great than 3:", s)
		}
	}

	sort.Ints(slots)
	tag := int64(0)
	for i := len(slots) - 1; i >= 0; i-- {
		if slots[i] == 0 {
			break
		}

		tag = tag*10 + int64(slots[i])
	}

	return tag
}

func assertCardHandType(hai []int, cht xproto.CardHandType) {
	tag := calcKeyTag(hai)
	v, ok := keyTags[tag]
	if !ok {
		log.Panicln("tag not valid:", tag)
	}

	// log.Printf("tag:%d, v:%d\n", tag, v)
	cht2 := (v & 0x0f)
	if cht2 != int(cht) {
		log.Panicln("CardHandType not exist:", cht)
	}

	// log.Println("equal:", cht)
}

func dumpKeysTag() {
	for k, v := range keyTags {
		fmt.Printf("patternTable[0x%x]=0x%x\n", k, v)
	}
}

func main() {
	log.Println("shisanshui pattern gen")

	genAllTag()

	dumpKeysTag()

	log.Printf("tag count:%d\n", len(keyTags))
}
