package tables

import (
	"sync"
)

var (
	// Mgr table manager singleton instance
	Mgr *TableMgr
)

// TableMgr table manager
type TableMgr struct {
	tablesMap   sync.Map
	tablesIDMap sync.Map
}

// GetTableByNumber get table by table number
func (m *TableMgr) GetTableByNumber(tableNumber string) *Table {
	tuid, ok := m.tablesIDMap.Load(tableNumber)
	if ok {
		return m.GetTable(tuid.(string))
	}

	return nil
}

// GetTable get table by uuid
func (m *TableMgr) GetTable(tableID string) *Table {
	t, ok := m.tablesMap.Load(tableID)
	if ok {
		return t.(*Table)
	}

	return nil
}
