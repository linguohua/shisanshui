package tables

// tableConfig table cofig
type tableConfig struct {
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
