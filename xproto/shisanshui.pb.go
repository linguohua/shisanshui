// Code generated by protoc-gen-go. DO NOT EDIT.
// versions:
// 	protoc-gen-go v1.25.0
// 	protoc        v3.12.4
// source: shisanshui.proto

package xproto

import (
	proto "github.com/golang/protobuf/proto"
	protoreflect "google.golang.org/protobuf/reflect/protoreflect"
	protoimpl "google.golang.org/protobuf/runtime/protoimpl"
	reflect "reflect"
	sync "sync"
)

const (
	// Verify that this generated code is sufficiently up-to-date.
	_ = protoimpl.EnforceVersion(20 - protoimpl.MinVersion)
	// Verify that runtime/protoimpl is sufficiently up-to-date.
	_ = protoimpl.EnforceVersion(protoimpl.MaxVersion - 20)
)

// This is a compile-time assertion that a sufficiently up-to-date version
// of the legacy proto package is being used.
const _ = proto.ProtoPackageIsVersion4

// 牌组类型
type CardHandType int32

const (
	CardHandType_None CardHandType = 0 // 无效牌型
	// 同花顺	Straight Flush 五张或更多的连续单牌（如： 45678 或 78910JQK ）
	CardHandType_StraightFlush CardHandType = 1
	// 四条 Four of a Kind：四张同点牌 + 一张
	CardHandType_Four CardHandType = 2
	// 葫芦
	CardHandType_FullHouse CardHandType = 3
	// 同花(花)	Flush
	CardHandType_Flush CardHandType = 4
	// 顺子(蛇)	Straight
	CardHandType_Straight CardHandType = 5
	// 三条 Three of a kind
	CardHandType_ThreeOfAKind CardHandType = 6
	// 两对牌：数值相同的两张牌
	CardHandType_TwoPairs CardHandType = 7
	// 对牌
	CardHandType_OnePair CardHandType = 8
	// 单张
	CardHandType_HighCard CardHandType = 9
)

// Enum value maps for CardHandType.
var (
	CardHandType_name = map[int32]string{
		0: "None",
		1: "StraightFlush",
		2: "Four",
		3: "FullHouse",
		4: "Flush",
		5: "Straight",
		6: "ThreeOfAKind",
		7: "TwoPairs",
		8: "OnePair",
		9: "HighCard",
	}
	CardHandType_value = map[string]int32{
		"None":          0,
		"StraightFlush": 1,
		"Four":          2,
		"FullHouse":     3,
		"Flush":         4,
		"Straight":      5,
		"ThreeOfAKind":  6,
		"TwoPairs":      7,
		"OnePair":       8,
		"HighCard":      9,
	}
)

func (x CardHandType) Enum() *CardHandType {
	p := new(CardHandType)
	*p = x
	return p
}

func (x CardHandType) String() string {
	return protoimpl.X.EnumStringOf(x.Descriptor(), protoreflect.EnumNumber(x))
}

func (CardHandType) Descriptor() protoreflect.EnumDescriptor {
	return file_shisanshui_proto_enumTypes[0].Descriptor()
}

func (CardHandType) Type() protoreflect.EnumType {
	return &file_shisanshui_proto_enumTypes[0]
}

func (x CardHandType) Number() protoreflect.EnumNumber {
	return protoreflect.EnumNumber(x)
}

// Deprecated: Do not use.
func (x *CardHandType) UnmarshalJSON(b []byte) error {
	num, err := protoimpl.X.UnmarshalJSONEnum(x.Descriptor(), b)
	if err != nil {
		return err
	}
	*x = CardHandType(num)
	return nil
}

// Deprecated: Use CardHandType.Descriptor instead.
func (CardHandType) EnumDescriptor() ([]byte, []int) {
	return file_shisanshui_proto_rawDescGZIP(), []int{0}
}

// 一手牌局结束
// 可能的结果是：流局、有人赢牌
type HandOverType int32

const (
	// 没有胡牌，或者流局
	HandOverType_enumHandOverType_None HandOverType = 0
	// 赢牌
	HandOverType_enumHandOverType_Win HandOverType = 1
	// 输牌
	HandOverType_enumHandOverType_Loss HandOverType = 2
)

// Enum value maps for HandOverType.
var (
	HandOverType_name = map[int32]string{
		0: "enumHandOverType_None",
		1: "enumHandOverType_Win",
		2: "enumHandOverType_Loss",
	}
	HandOverType_value = map[string]int32{
		"enumHandOverType_None": 0,
		"enumHandOverType_Win":  1,
		"enumHandOverType_Loss": 2,
	}
)

func (x HandOverType) Enum() *HandOverType {
	p := new(HandOverType)
	*p = x
	return p
}

func (x HandOverType) String() string {
	return protoimpl.X.EnumStringOf(x.Descriptor(), protoreflect.EnumNumber(x))
}

func (HandOverType) Descriptor() protoreflect.EnumDescriptor {
	return file_shisanshui_proto_enumTypes[1].Descriptor()
}

func (HandOverType) Type() protoreflect.EnumType {
	return &file_shisanshui_proto_enumTypes[1]
}

func (x HandOverType) Number() protoreflect.EnumNumber {
	return protoreflect.EnumNumber(x)
}

// Deprecated: Do not use.
func (x *HandOverType) UnmarshalJSON(b []byte) error {
	num, err := protoimpl.X.UnmarshalJSONEnum(x.Descriptor(), b)
	if err != nil {
		return err
	}
	*x = HandOverType(num)
	return nil
}

// Deprecated: Use HandOverType.Descriptor instead.
func (HandOverType) EnumDescriptor() ([]byte, []int) {
	return file_shisanshui_proto_rawDescGZIP(), []int{1}
}

// 动作类型
// 注意为了能够用一个int合并多个动作
// 因此所有动作的值均为二进制bit field独立
type ActionType int32

const (
	// 无效动作
	ActionType_enumActionType_None ActionType = 0
	// 出牌
	ActionType_enumActionType_DISCARD ActionType = 1
)

// Enum value maps for ActionType.
var (
	ActionType_name = map[int32]string{
		0: "enumActionType_None",
		1: "enumActionType_DISCARD",
	}
	ActionType_value = map[string]int32{
		"enumActionType_None":    0,
		"enumActionType_DISCARD": 1,
	}
)

func (x ActionType) Enum() *ActionType {
	p := new(ActionType)
	*p = x
	return p
}

func (x ActionType) String() string {
	return protoimpl.X.EnumStringOf(x.Descriptor(), protoreflect.EnumNumber(x))
}

func (ActionType) Descriptor() protoreflect.EnumDescriptor {
	return file_shisanshui_proto_enumTypes[2].Descriptor()
}

func (ActionType) Type() protoreflect.EnumType {
	return &file_shisanshui_proto_enumTypes[2]
}

func (x ActionType) Number() protoreflect.EnumNumber {
	return protoreflect.EnumNumber(x)
}

// Deprecated: Do not use.
func (x *ActionType) UnmarshalJSON(b []byte) error {
	num, err := protoimpl.X.UnmarshalJSONEnum(x.Descriptor(), b)
	if err != nil {
		return err
	}
	*x = ActionType(num)
	return nil
}

// Deprecated: Use ActionType.Descriptor instead.
func (ActionType) EnumDescriptor() ([]byte, []int) {
	return file_shisanshui_proto_rawDescGZIP(), []int{2}
}

var File_shisanshui_proto protoreflect.FileDescriptor

var file_shisanshui_proto_rawDesc = []byte{
	0x0a, 0x10, 0x73, 0x68, 0x69, 0x73, 0x61, 0x6e, 0x73, 0x68, 0x75, 0x69, 0x2e, 0x70, 0x72, 0x6f,
	0x74, 0x6f, 0x12, 0x06, 0x78, 0x70, 0x72, 0x6f, 0x74, 0x6f, 0x2a, 0x98, 0x01, 0x0a, 0x0c, 0x43,
	0x61, 0x72, 0x64, 0x48, 0x61, 0x6e, 0x64, 0x54, 0x79, 0x70, 0x65, 0x12, 0x08, 0x0a, 0x04, 0x4e,
	0x6f, 0x6e, 0x65, 0x10, 0x00, 0x12, 0x11, 0x0a, 0x0d, 0x53, 0x74, 0x72, 0x61, 0x69, 0x67, 0x68,
	0x74, 0x46, 0x6c, 0x75, 0x73, 0x68, 0x10, 0x01, 0x12, 0x08, 0x0a, 0x04, 0x46, 0x6f, 0x75, 0x72,
	0x10, 0x02, 0x12, 0x0d, 0x0a, 0x09, 0x46, 0x75, 0x6c, 0x6c, 0x48, 0x6f, 0x75, 0x73, 0x65, 0x10,
	0x03, 0x12, 0x09, 0x0a, 0x05, 0x46, 0x6c, 0x75, 0x73, 0x68, 0x10, 0x04, 0x12, 0x0c, 0x0a, 0x08,
	0x53, 0x74, 0x72, 0x61, 0x69, 0x67, 0x68, 0x74, 0x10, 0x05, 0x12, 0x10, 0x0a, 0x0c, 0x54, 0x68,
	0x72, 0x65, 0x65, 0x4f, 0x66, 0x41, 0x4b, 0x69, 0x6e, 0x64, 0x10, 0x06, 0x12, 0x0c, 0x0a, 0x08,
	0x54, 0x77, 0x6f, 0x50, 0x61, 0x69, 0x72, 0x73, 0x10, 0x07, 0x12, 0x0b, 0x0a, 0x07, 0x4f, 0x6e,
	0x65, 0x50, 0x61, 0x69, 0x72, 0x10, 0x08, 0x12, 0x0c, 0x0a, 0x08, 0x48, 0x69, 0x67, 0x68, 0x43,
	0x61, 0x72, 0x64, 0x10, 0x09, 0x2a, 0x5e, 0x0a, 0x0c, 0x48, 0x61, 0x6e, 0x64, 0x4f, 0x76, 0x65,
	0x72, 0x54, 0x79, 0x70, 0x65, 0x12, 0x19, 0x0a, 0x15, 0x65, 0x6e, 0x75, 0x6d, 0x48, 0x61, 0x6e,
	0x64, 0x4f, 0x76, 0x65, 0x72, 0x54, 0x79, 0x70, 0x65, 0x5f, 0x4e, 0x6f, 0x6e, 0x65, 0x10, 0x00,
	0x12, 0x18, 0x0a, 0x14, 0x65, 0x6e, 0x75, 0x6d, 0x48, 0x61, 0x6e, 0x64, 0x4f, 0x76, 0x65, 0x72,
	0x54, 0x79, 0x70, 0x65, 0x5f, 0x57, 0x69, 0x6e, 0x10, 0x01, 0x12, 0x19, 0x0a, 0x15, 0x65, 0x6e,
	0x75, 0x6d, 0x48, 0x61, 0x6e, 0x64, 0x4f, 0x76, 0x65, 0x72, 0x54, 0x79, 0x70, 0x65, 0x5f, 0x4c,
	0x6f, 0x73, 0x73, 0x10, 0x02, 0x2a, 0x41, 0x0a, 0x0a, 0x41, 0x63, 0x74, 0x69, 0x6f, 0x6e, 0x54,
	0x79, 0x70, 0x65, 0x12, 0x17, 0x0a, 0x13, 0x65, 0x6e, 0x75, 0x6d, 0x41, 0x63, 0x74, 0x69, 0x6f,
	0x6e, 0x54, 0x79, 0x70, 0x65, 0x5f, 0x4e, 0x6f, 0x6e, 0x65, 0x10, 0x00, 0x12, 0x1a, 0x0a, 0x16,
	0x65, 0x6e, 0x75, 0x6d, 0x41, 0x63, 0x74, 0x69, 0x6f, 0x6e, 0x54, 0x79, 0x70, 0x65, 0x5f, 0x44,
	0x49, 0x53, 0x43, 0x41, 0x52, 0x44, 0x10, 0x01, 0x42, 0x0a, 0x5a, 0x08, 0x2e, 0x3b, 0x78, 0x70,
	0x72, 0x6f, 0x74, 0x6f,
}

var (
	file_shisanshui_proto_rawDescOnce sync.Once
	file_shisanshui_proto_rawDescData = file_shisanshui_proto_rawDesc
)

func file_shisanshui_proto_rawDescGZIP() []byte {
	file_shisanshui_proto_rawDescOnce.Do(func() {
		file_shisanshui_proto_rawDescData = protoimpl.X.CompressGZIP(file_shisanshui_proto_rawDescData)
	})
	return file_shisanshui_proto_rawDescData
}

var file_shisanshui_proto_enumTypes = make([]protoimpl.EnumInfo, 3)
var file_shisanshui_proto_goTypes = []interface{}{
	(CardHandType)(0), // 0: xproto.CardHandType
	(HandOverType)(0), // 1: xproto.HandOverType
	(ActionType)(0),   // 2: xproto.ActionType
}
var file_shisanshui_proto_depIdxs = []int32{
	0, // [0:0] is the sub-list for method output_type
	0, // [0:0] is the sub-list for method input_type
	0, // [0:0] is the sub-list for extension type_name
	0, // [0:0] is the sub-list for extension extendee
	0, // [0:0] is the sub-list for field type_name
}

func init() { file_shisanshui_proto_init() }
func file_shisanshui_proto_init() {
	if File_shisanshui_proto != nil {
		return
	}
	type x struct{}
	out := protoimpl.TypeBuilder{
		File: protoimpl.DescBuilder{
			GoPackagePath: reflect.TypeOf(x{}).PkgPath(),
			RawDescriptor: file_shisanshui_proto_rawDesc,
			NumEnums:      3,
			NumMessages:   0,
			NumExtensions: 0,
			NumServices:   0,
		},
		GoTypes:           file_shisanshui_proto_goTypes,
		DependencyIndexes: file_shisanshui_proto_depIdxs,
		EnumInfos:         file_shisanshui_proto_enumTypes,
	}.Build()
	File_shisanshui_proto = out.File
	file_shisanshui_proto_rawDesc = nil
	file_shisanshui_proto_goTypes = nil
	file_shisanshui_proto_depIdxs = nil
}
