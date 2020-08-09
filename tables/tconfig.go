package tables

import (
	"encoding/json"
	"fmt"
	"strings"

	"github.com/DisposaBoy/JsonConfigReader"
)

// tableConfig table cofig
// 每个牌桌都需要一个配置，配置指定了各种游戏规则
// 牌桌规则由大厅服务器下发一个ID给游戏服务器，然后由游戏服务器根据该ID去redis
// 完整的配置
type tableConfig struct {
	ID string `json:"id,omitempty"`

	PlayerNumAcquired int `json:"playerAcquired"`
	PlayerNumMax      int `json:"playerMax"`

	Countdown int `json:"countdown"`
}

func tableConfigNew() *tableConfig {
	return &tableConfig{}
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

// monkey config
type monkeyConfig struct {
	name                   string
	isForceConsistent      bool
	monkeyUserCardsCfgList []*monkeyCardConfig
}

// monkey card config
type monkeyCardConfig struct {
	chairID int
	userID  string
}

func monkeyConfigNewFromCSV(csvStr string) (*monkeyConfig, error) {
	return nil, fmt.Errorf("not implement")
}

func (mc *monkeyConfig) playerCount() int {
	// TODO:
	return 0
}

func (mc *monkeyConfig) getMonkeyCardCfg(userID string) *monkeyCardConfig {
	return nil
}
