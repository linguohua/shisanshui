package tables

import (
	"encoding/csv"
	"encoding/json"
	"fmt"
	"io"
	"shisanshui/xproto"
	"strings"

	"github.com/DisposaBoy/JsonConfigReader"
	"github.com/sirupsen/logrus"
)

var (
	// card name to card id
	dict = make(map[string]int)
	// card id to card name
	dictName = make(map[int]string)
)

// monkey config
type monkeyConfig struct {
	// config name
	name string
	// if it is true, then all player id must match
	// userIDs in cfg
	isForceConsistent bool
	// each player's card list config
	monkeyCardsCfgList []*monkeyCardConfig
}

// monkey card config
type monkeyCardConfig struct {
	userID string

	// card list
	handCards []int
	// use in replay
	actionTips []string

	isBanker bool
	index    int
}

func tableConfigNewFromJSON(jsonString string) (*tableConfig, error) {
	var cfg = &tableConfig{}

	// wrap our reader before passing it to the json decoder
	reader := JsonConfigReader.New(strings.NewReader(jsonString))
	err := json.NewDecoder(reader).Decode(cfg)
	if err != nil {
		return nil, err
	}

	return cfg, nil
}

func monkeyConfigNew(name string) *monkeyConfig {
	mc := &monkeyConfig{}
	mc.name = name
	mc.monkeyCardsCfgList = make([]*monkeyCardConfig, 0, 4)

	return mc
}

func monkeyCardConfigNew(isBanker bool, idx int) *monkeyCardConfig {
	tuc := monkeyCardConfig{}
	tuc.isBanker = isBanker
	tuc.index = idx
	return &tuc
}

func (mc *monkeyConfig) playerCount() int {
	return len(mc.monkeyCardsCfgList)
}

func (mc *monkeyConfig) getMonkeyCardCfg(userID string) *monkeyCardConfig {
	for _, mcc := range mc.monkeyCardsCfgList {
		if mcc.userID == userID {
			return mcc
		}
	}

	return nil
}

func monkeyConfigNewFromCSV(csvStr string, cl *logrus.Entry) (*monkeyConfig, error) {
	textReader := strings.NewReader(csvStr)
	reader := csv.NewReader(textReader)

	// 跳过头部第一行
	record, err := reader.Read()
	err = monkeyConfigVerifyHeader(record)
	if err != nil {
		cl.Println("verifyHeader:", err.Error())
		return nil, err
	}

	for {
		record, err = reader.Read()
		// Stop at EOF.
		if err == io.EOF {
			break
		}

		if len(record) == 0 {
			continue
		}

		var name = record[0]
		if name == "" {
			continue
		}

		var ttype = record[1]
		if ttype != "十三水" {
			continue
		}

		//log.Println("cfg name: ", name)
		var cfg = monkeyConfigNew(name)

		// 4 个玩家的手牌，花牌，和动作提示
		for i := 0; i < 4; i++ {
			tuc := monkeyConfigExtractUserCardsCfg(record, i)
			if tuc != nil {
				cfg.monkeyCardsCfgList = append(cfg.monkeyCardsCfgList, tuc)
			}
		}

		var forceConsistent = strings.Trim(record[14], "\t ")
		if forceConsistent == "1" {
			cfg.isForceConsistent = true
		}

		if cfg.isValid(cl) {
			return cfg, nil
		}

		err = fmt.Errorf("config %s invalid", name)
		return nil, err
	}

	return nil, nil
}

func monkeyConfigVerifyHeader(record []string) error {
	headers := []string{"名称", "类型", "庄家userID", "庄家手牌", "庄家动作提示", "userID2", "手牌", "动作提示", "userID3", "手牌",
		"动作提示", "userID4", "手牌", "动作提示", "强制一致", "房间配置ID"}

	if len(headers) != len(record) {
		return fmt.Errorf("csv file not match, maybe you use old versoin pokerface test client, input header length:%d, require:%d", len(record), len(headers))
	}

	for i, h := range headers {
		if h != record[i] {
			return fmt.Errorf("csv file not match, maybe you use old versoin pokerface test client, %s != %s", h, record[i])
		}
	}

	return nil
}

// monkeyConfigExtractUserCardsCfg 从csv文件里面抽取玩家发牌配置
func monkeyConfigExtractUserCardsCfg(record []string, userIndex int) *monkeyCardConfig {
	beginIdx := userIndex*3 + 2
	mcc := monkeyCardConfigNew(userIndex == 0, userIndex)

	// userID
	userID := record[beginIdx]
	// if userID == "" {
	// 	return nil
	// }

	mcc.userID = userID
	// 手牌
	handcomps := record[beginIdx+1]
	handcomps = strings.Trim(handcomps, " \t")
	if handcomps == "" {
		// 必须有手牌配置
		return nil
	}
	handcomps = strings.Replace(handcomps, "，", ",", -1)
	var hands = strings.Split(handcomps, ",")
	monkeyConfigTrimLeftRight(hands)
	if len(hands) >= 13 {
		mcc.setHandCards(hands)
	}

	// 动作提示
	tipscomps := record[beginIdx+2]
	tipscomps = strings.Trim(tipscomps, " \t")
	if tipscomps != "" {
		tipscomps = strings.Replace(tipscomps, "，", ",", -1)
		var tips = strings.Split(tipscomps, ",")
		if len(tips) > 0 {
			monkeyConfigTrimLeftRight(tips)
			mcc.setActionTips(tips)
		}
	}

	return mcc
}

// trimLeftRight 从左，从右裁剪所有数组中的字符串
func monkeyConfigTrimLeftRight(draws []string) {
	for index, draw := range draws {
		draws[index] = strings.Trim(draw, " \t")
	}
}

// isValid 判断配置是否有效
func (mc *monkeyConfig) isValid(cl *logrus.Entry) bool {
	for _, cardsUserCfg := range mc.monkeyCardsCfgList {
		if !cardsUserCfg.isValid() {
			cl.Println("!cardsUserCfg.isValid()")
			return false
		}
	}

	if len(mc.monkeyCardsCfgList) < 2 {
		cl.Println("len(mtc.cardsPairList) < 2")
		return false
	}

	// 检查牌的张数是否在要求范围内
	slots := make([]int, xproto.CardID_CARDMAX)
	for _, tuc := range mc.monkeyCardsCfgList {
		for _, handCard := range tuc.handCards {
			slots[handCard]++
		}
	}

	// 每一种牌，最多有1张
	for tid, slot := range slots {
		if slot > 1 {
			cl.Println("slot > 1 :", tid)
			return false
		}
	}

	return true
}

// isValid 判断手牌是否有效
func (mcc *monkeyCardConfig) isValid() bool {
	if mcc.handCards != nil && len(mcc.handCards) == 13 {
		return true
	}

	return false
}

// setHandCards 设置发牌时的手牌序列
func (mcc *monkeyCardConfig) setHandCards(hands []string) {

	var index = 0
	hl := make([]int, len(hands))
	for _, hand := range hands {
		if hand != "" {
			hl[index] = dict[hand]
			index++
		}
	}

	mcc.handCards = hl[0:index]
}

// setActionTips 设置动作提示，用于提示客户端下一步动作是什么
func (mcc *monkeyCardConfig) setActionTips(actionTips []string) {

	var index = 0
	tips := make([]string, len(actionTips))
	for _, tip := range actionTips {
		if tip != "" {
			tips[index] = tip
			index++
		}
	}

	mcc.actionTips = tips[0:index]
}

// initDict 初始化两个map，用于转换牌的字符到序号
func initDict() {
	dict["红桃2"] = int(xproto.CardID_R2H)
	dict["方块2"] = int(xproto.CardID_R2D)
	dict["梅花2"] = int(xproto.CardID_R2C)
	dict["黑桃2"] = int(xproto.CardID_R2S)

	dict["红桃3"] = int(xproto.CardID_R3H)
	dict["方块3"] = int(xproto.CardID_R3D)
	dict["梅花3"] = int(xproto.CardID_R3C)
	dict["黑桃3"] = int(xproto.CardID_R3S)

	dict["红桃4"] = int(xproto.CardID_R4H)
	dict["方块4"] = int(xproto.CardID_R4D)
	dict["梅花4"] = int(xproto.CardID_R4C)
	dict["黑桃4"] = int(xproto.CardID_R4S)

	dict["红桃5"] = int(xproto.CardID_R5H)
	dict["方块5"] = int(xproto.CardID_R5D)
	dict["梅花5"] = int(xproto.CardID_R5C)
	dict["黑桃5"] = int(xproto.CardID_R5S)

	dict["红桃6"] = int(xproto.CardID_R6H)
	dict["方块6"] = int(xproto.CardID_R6D)
	dict["梅花6"] = int(xproto.CardID_R6C)
	dict["黑桃6"] = int(xproto.CardID_R6S)

	dict["红桃7"] = int(xproto.CardID_R7H)
	dict["方块7"] = int(xproto.CardID_R7D)
	dict["梅花7"] = int(xproto.CardID_R7C)
	dict["黑桃7"] = int(xproto.CardID_R7S)

	dict["红桃8"] = int(xproto.CardID_R8H)
	dict["方块8"] = int(xproto.CardID_R8D)
	dict["梅花8"] = int(xproto.CardID_R8C)
	dict["黑桃8"] = int(xproto.CardID_R8S)

	dict["红桃9"] = int(xproto.CardID_R9H)
	dict["方块9"] = int(xproto.CardID_R9D)
	dict["梅花9"] = int(xproto.CardID_R9C)
	dict["黑桃9"] = int(xproto.CardID_R9S)

	dict["红桃10"] = int(xproto.CardID_R10H)
	dict["方块10"] = int(xproto.CardID_R10D)
	dict["梅花10"] = int(xproto.CardID_R10C)
	dict["黑桃10"] = int(xproto.CardID_R10S)

	dict["红桃J"] = int(xproto.CardID_JH)
	dict["方块J"] = int(xproto.CardID_JD)
	dict["梅花J"] = int(xproto.CardID_JC)
	dict["黑桃J"] = int(xproto.CardID_JS)

	dict["红桃Q"] = int(xproto.CardID_QH)
	dict["方块Q"] = int(xproto.CardID_QD)
	dict["梅花Q"] = int(xproto.CardID_QC)
	dict["黑桃Q"] = int(xproto.CardID_QS)

	dict["红桃K"] = int(xproto.CardID_KH)
	dict["方块K"] = int(xproto.CardID_KD)
	dict["梅花K"] = int(xproto.CardID_KC)
	dict["黑桃K"] = int(xproto.CardID_KS)

	dict["红桃A"] = int(xproto.CardID_AH)
	dict["方块A"] = int(xproto.CardID_AD)
	dict["梅花A"] = int(xproto.CardID_AC)
	dict["黑桃A"] = int(xproto.CardID_AS)

	dict["黑小丑"] = int(xproto.CardID_JOB)
	dict["红小丑"] = int(xproto.CardID_JOR)

	dictName[int(xproto.CardID_R2H)] = "红桃2"
	dictName[int(xproto.CardID_R2D)] = "方块2"
	dictName[int(xproto.CardID_R2C)] = "梅花2"
	dictName[int(xproto.CardID_R2S)] = "黑桃2"

	dictName[int(xproto.CardID_R3H)] = "红桃3"
	dictName[int(xproto.CardID_R3D)] = "方块3"
	dictName[int(xproto.CardID_R3C)] = "梅花3"
	dictName[int(xproto.CardID_R3S)] = "黑桃3"

	dictName[int(xproto.CardID_R4H)] = "红桃4"
	dictName[int(xproto.CardID_R4D)] = "方块4"
	dictName[int(xproto.CardID_R4C)] = "梅花4"
	dictName[int(xproto.CardID_R4S)] = "黑桃4"

	dictName[int(xproto.CardID_R5H)] = "红桃5"
	dictName[int(xproto.CardID_R5D)] = "方块5"
	dictName[int(xproto.CardID_R5C)] = "梅花5"
	dictName[int(xproto.CardID_R5S)] = "黑桃5"

	dictName[int(xproto.CardID_R6H)] = "红桃6"
	dictName[int(xproto.CardID_R6D)] = "方块6"
	dictName[int(xproto.CardID_R6C)] = "梅花6"
	dictName[int(xproto.CardID_R6S)] = "黑桃6"

	dictName[int(xproto.CardID_R7H)] = "红桃7"
	dictName[int(xproto.CardID_R7D)] = "方块7"
	dictName[int(xproto.CardID_R7C)] = "梅花7"
	dictName[int(xproto.CardID_R7S)] = "黑桃7"

	dictName[int(xproto.CardID_R8H)] = "红桃8"
	dictName[int(xproto.CardID_R8D)] = "方块8"
	dictName[int(xproto.CardID_R8C)] = "梅花8"
	dictName[int(xproto.CardID_R8S)] = "黑桃8"

	dictName[int(xproto.CardID_R9H)] = "红桃9"
	dictName[int(xproto.CardID_R9D)] = "方块9"
	dictName[int(xproto.CardID_R9C)] = "梅花9"
	dictName[int(xproto.CardID_R9S)] = "黑桃9"

	dictName[int(xproto.CardID_R10H)] = "红桃10"
	dictName[int(xproto.CardID_R10D)] = "方块10"
	dictName[int(xproto.CardID_R10C)] = "梅花10"
	dictName[int(xproto.CardID_R10S)] = "黑桃10"

	dictName[int(xproto.CardID_JH)] = "红桃J"
	dictName[int(xproto.CardID_JD)] = "方块J"
	dictName[int(xproto.CardID_JC)] = "梅花J"
	dictName[int(xproto.CardID_JS)] = "黑桃J"

	dictName[int(xproto.CardID_QH)] = "红桃Q"
	dictName[int(xproto.CardID_QD)] = "方块Q"
	dictName[int(xproto.CardID_QC)] = "梅花Q"
	dictName[int(xproto.CardID_QS)] = "黑桃Q"

	dictName[int(xproto.CardID_KH)] = "红桃K"
	dictName[int(xproto.CardID_KD)] = "方块K"
	dictName[int(xproto.CardID_KC)] = "梅花K"
	dictName[int(xproto.CardID_KS)] = "黑桃K"

	dictName[int(xproto.CardID_AH)] = "红桃A"
	dictName[int(xproto.CardID_AD)] = "方块A"
	dictName[int(xproto.CardID_AC)] = "梅花A"
	dictName[int(xproto.CardID_AS)] = "黑桃A"

	dictName[int(xproto.CardID_JOB)] = "黑小丑"
	dictName[int(xproto.CardID_JOR)] = "红小丑"
}

func init() {
	initDict()
}
