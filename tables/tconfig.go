package tables

// tableConfig table cofig
// 每个牌桌都需要一个配置，配置指定了各种游戏规则
// 牌桌规则由大厅服务器下发一个ID给游戏服务器，然后由游戏服务器根据该ID去redis
// 完整的配置
type tableConfig struct {
	ID string

	playerNumAcquired int
	playerNumMax      int

	Countdown int
}

// monkey config
type monkeyConfig struct {
}

// monkey card config
type monkeyCardConfig struct {
	chairID int
}

func (mc *monkeyConfig) getMonkeyCardCfg(userID string) *monkeyCardConfig {
	return nil
}
