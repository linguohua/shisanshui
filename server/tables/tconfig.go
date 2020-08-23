package tables

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
	return &tableConfig{
		PlayerNumAcquired: 2,
		PlayerNumMax:      4,
		Countdown:         5,
	}
}
