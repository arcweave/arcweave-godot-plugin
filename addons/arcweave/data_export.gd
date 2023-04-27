extends Object
class_name DataExport

var starting_element: String = ""
var name: String = ""
var utils: Utils
var data = {
	"startingElement": "38375689-9e4d-430e-954c-65205eb622fe",
	"boards": {
		"2ad15ff6-f2ba-4f1d-9d9d-68938c683d7e": {
			"name": "Root",
			"root": true,
			"children": [
				"630fdb8a-48d6-473e-9974-2460f7eb2b41",
				"733b1cc4-6d51-4ad6-9fba-444b4f2d98f3"
			]
		},
		"630fdb8a-48d6-473e-9974-2460f7eb2b41": {
			"name": "Main Board",
			"notes": [
				"eda3ed5a-bdd5-4981-a72f-c7847630310a",
				"585c89a5-a16c-44db-9346-6e2725afcd88",
				"5f45b936-1a0f-4834-87c3-579fb72329b1",
				"0d580890-add4-4750-9b0c-64a9be4bd9be",
				"045fcf7a-0e29-4f29-a6b3-f1be825832ed",
				"27bcf38e-cef9-4413-b4c9-97e6c7300ec3",
				"c917680a-9884-41fb-832c-7ac41f87c520"
			],
			"jumpers": [
				"17251b2a-0930-4a93-8f14-e89eb3238146",
				"60b22737-d18c-44ea-b24c-8d771dfd9f9d"
			],
			"branches": [],
			"elements": [
				"38375689-9e4d-430e-954c-65205eb622fe",
				"59370898-e089-46cd-b7cf-072b79a76453",
				"5e0d8b0c-ae7c-42a6-83d1-fc2c13b8e6b4",
				"b2eb06f5-0a49-488b-8a60-7c469fd3af8a",
				"81a30674-aa09-4eb8-8484-59298d37f984",
				"188e6385-85e2-496b-82ac-a81696e7bcab"
			],
			"connections": [
				"b7f7dfc4-46f9-40ee-a8e9-e8b584cecadf",
				"e8fbe6f8-7ce4-404d-b693-d108d7125a75",
				"6f0952d7-202a-4978-8d63-3fee4c5dde35",
				"c903fd63-a07c-4d5f-829e-461eee7ce2b6",
				"8ec9d83d-02cd-41fe-a16b-a7773e1ec678",
				"337c4347-5b21-449c-b556-134e0acec12d",
				"678e093e-537c-4161-b011-8d6763c4be14",
				"9b8efafe-a73f-4c19-8b90-0cf18b7e4907",
				"3461cb48-901b-49d5-99ba-dbfb69e6efe1",
				"6a929b6d-c6d2-46c4-ab67-b0503e7213af",
				"bd5fbb0a-481c-4b6a-8c77-21b153c4bb95"
			]
		},
		"733b1cc4-6d51-4ad6-9fba-444b4f2d98f3": {
			"name": "Dialogues",
			"children": [
				"fde6c03b-9c62-4d2d-82b9-9d6613cf6c6f"
			]
		},
		"fde6c03b-9c62-4d2d-82b9-9d6613cf6c6f": {
			"name": "Crusader Dialogue",
			"notes": [
				"f9f032d4-60f6-43f8-bee4-c76fbfca9609"
			],
			"jumpers": [
				"3517a1bc-3b73-4850-b03d-09daf9b7bc89"
			],
			"branches": [
				"e4ef520d-1a30-4fd8-97f7-ed7f3ccb1426"
			],
			"elements": [
				"d4823bca-643b-4d3a-a908-0056762a0476",
				"601034d9-b442-449a-a927-cd112fca8f7e",
				"d393f371-5aad-4dd9-a3ee-300bedaf993d",
				"fa351fcb-f7cd-4b65-ae0a-4640e8a5275f",
				"4d61d93e-e541-4dff-b10d-42c4fd81ad0b",
				"4c7d10f2-e68b-498f-9ced-ffd74977a8f5",
				"b6b7cebe-0c30-4da2-9839-2639f458f759"
			],
			"connections": [
				"3bcf09a5-0680-4826-8fef-76e79f31fad4",
				"80407dcb-6a30-4720-8d87-0d53419c4d30",
				"6738d031-bbd5-4d98-993f-a4926fc7fa33",
				"ce4869fb-b31e-47a5-a0ca-47e46839a6f8",
				"dd563c04-f2d3-4c7f-a66e-3bb649c34831",
				"0c0cdd6d-3686-4198-88c7-d0753dda6945",
				"64b12ec3-e4d4-475b-ab3b-d1187b043a04",
				"1a09fe9a-062e-4dbb-8615-bf0aa455f783",
				"10b4a35e-8329-4a0d-86d2-3d0144254aa1",
				"a21bbb0e-2496-4cd9-81a5-08773b3a4044"
			]
		}
	},
	"notes": {
		"045fcf7a-0e29-4f29-a6b3-f1be825832ed": {
			"theme": "blue",
			"content": "Change the color of an element or note by [b]right clicking[/b] on it. You can also add image or icon covers to elements!",
			"autoHeight": true
		},
		"0d580890-add4-4750-9b0c-64a9be4bd9be": {
			"theme": "default",
			"content": "Create connections between elements by clicking and dragging near their perimeter. Add labels to connections by double clicking on them.",
			"autoHeight": true
		},
		"27bcf38e-cef9-4413-b4c9-97e6c7300ec3": {
			"theme": "purple",
			"content": "You can use [url=https://arcweave.com/docs/1.0/arcscript]Arcscript[/url] inside element content to set variables or conditionally display content in Play Mode.",
			"autoHeight": true
		},
		"585c89a5-a16c-44db-9346-6e2725afcd88": {
			"theme": "red",
			"content": "You can reference components and boards by @mentioning them.",
			"autoHeight": true
		},
		"5f45b936-1a0f-4834-87c3-579fb72329b1": {
			"theme": "lightBlue",
			"content": "[b]Welcome to Arcweave![/b]\\nDouble click on an empty spot in the workspace to create an element. To edit it double click on its title or content.",
			"autoHeight": true
		},
		"c917680a-9884-41fb-832c-7ac41f87c520": {
			"theme": "cyan",
			"content": "Attach components to elements by dragging and dropping them from the sidebar.",
			"autoHeight": true
		},
		"eda3ed5a-bdd5-4981-a72f-c7847630310a": {
			"theme": "green",
			"content": "You can create jumpers (links to elements) by dragging and dropping elements from the sidebar on the board.",
			"autoHeight": true
		},
		"f9f032d4-60f6-43f8-bee4-c76fbfca9609": {
			"theme": "lightBlue",
			"content": "Click on the \"Play Mode\" button to interactively test the experience!\\nProject images by [url=https://rachelchen.itch.io/]https://rachelchen.itch.io/[/url]",
			"autoHeight": true
		}
	},
	"elements": {
		"188e6385-85e2-496b-82ac-a81696e7bcab": {
			"theme": "cyan",
			"title": "The Crusader ",
			"content": "TO_BE_REPLACED",
			"outputs": [
				"3461cb48-901b-49d5-99ba-dbfb69e6efe1",
				"bd5fbb0a-481c-4b6a-8c77-21b153c4bb95"
			],
			"autoHeight": true,
			"components": [
				"647349ca-9ebb-4d5f-b81e-500761ec1c5a"
			],
			"contentRef": "element_0_content"
		},
		"38375689-9e4d-430e-954c-65205eb622fe": {
			"theme": "default",
			"title": "Outside Castle Monsignor",
			"content": "TO_BE_REPLACED",
			"outputs": [
				"b7f7dfc4-46f9-40ee-a8e9-e8b584cecadf"
			],
			"autoHeight": true,
			"components": [],
			"contentRef": "element_1_content",
			"cover": {
				"type": "image",
				"file": "/monsignor.jpeg"
			}
		},
		"4c7d10f2-e68b-498f-9ced-ffd74977a8f5": {
			"theme": "red",
			"title": "Wrong answer",
			"content": "TO_BE_REPLACED",
			"outputs": [
				"6738d031-bbd5-4d98-993f-a4926fc7fa33"
			],
			"autoHeight": true,
			"components": [],
			"contentRef": "element_2_content"
		},
		"4d61d93e-e541-4dff-b10d-42c4fd81ad0b": {
			"theme": "red",
			"title": "Game over",
			"content": "TO_BE_REPLACED",
			"outputs": [],
			"autoHeight": true,
			"components": [],
			"contentRef": "element_3_content",
			"cover": {
				"type": "image",
				"file": "/the end.jpeg"
			}
		},
		"59370898-e089-46cd-b7cf-072b79a76453": {
			"theme": "green",
			"title": "Castle Courtyard ",
			"content": "TO_BE_REPLACED",
			"outputs": [
				"678e093e-537c-4161-b011-8d6763c4be14",
				"e8fbe6f8-7ce4-404d-b693-d108d7125a75",
				"337c4347-5b21-449c-b556-134e0acec12d"
			],
			"autoHeight": true,
			"components": [],
			"contentRef": "element_4_content",
			"cover": {
				"type": "image",
				"file": "/courtyard.jpeg"
			}
		},
		"5e0d8b0c-ae7c-42a6-83d1-fc2c13b8e6b4": {
			"theme": "purple",
			"title": "The small house - Overview",
			"content": "TO_BE_REPLACED",
			"outputs": [
				"c903fd63-a07c-4d5f-829e-461eee7ce2b6",
				"6f0952d7-202a-4978-8d63-3fee4c5dde35"
			],
			"autoHeight": true,
			"components": [],
			"contentRef": "element_5_content",
			"cover": {
				"type": "image",
				"file": "/painter room.jpeg"
			}
		},
		"601034d9-b442-449a-a927-cd112fca8f7e": {
			"theme": "red",
			"title": "Trying to run past",
			"content": "TO_BE_REPLACED",
			"outputs": [
				"80407dcb-6a30-4720-8d87-0d53419c4d30"
			],
			"autoHeight": true,
			"components": [],
			"contentRef": "element_6_content"
		},
		"81a30674-aa09-4eb8-8484-59298d37f984": {
			"theme": "brown",
			"title": "The stone passage",
			"content": "TO_BE_REPLACED",
			"outputs": [
				"9b8efafe-a73f-4c19-8b90-0cf18b7e4907",
				"6a929b6d-c6d2-46c4-ab67-b0503e7213af"
			],
			"autoHeight": true,
			"components": [],
			"contentRef": "element_7_content",
			"cover": {
				"type": "image",
				"file": "/corridor.jpeg"
			}
		},
		"b2eb06f5-0a49-488b-8a60-7c469fd3af8a": {
			"theme": "purple",
			"title": "The small house - Examine the painting",
			"content": "TO_BE_REPLACED",
			"outputs": [
				"8ec9d83d-02cd-41fe-a16b-a7773e1ec678"
			],
			"autoHeight": true,
			"components": [],
			"contentRef": "element_8_content",
			"cover": {
				"type": "image",
				"file": "/painter room.jpeg"
			}
		},
		"b6b7cebe-0c30-4da2-9839-2639f458f759": {
			"theme": "green",
			"title": "Success",
			"content": "TO_BE_REPLACED",
			"outputs": [],
			"autoHeight": true,
			"components": [],
			"contentRef": "element_9_content",
			"cover": {
				"type": "image",
				"file": "/the end.jpeg"
			}
		},
		"d393f371-5aad-4dd9-a3ee-300bedaf993d": {
			"theme": "cyan",
			"title": "The Question",
			"content": "TO_BE_REPLACED",
			"outputs": [
				"64b12ec3-e4d4-475b-ab3b-d1187b043a04",
				"a21bbb0e-2496-4cd9-81a5-08773b3a4044",
				"dd563c04-f2d3-4c7f-a66e-3bb649c34831"
			],
			"autoHeight": true,
			"components": [],
			"contentRef": "element_10_content",
			"cover": {
				"type": "image",
				"file": "/crusader.jpeg"
			}
		},
		"d4823bca-643b-4d3a-a908-0056762a0476": {
			"theme": "cyan",
			"title": "Dialogue start",
			"content": "TO_BE_REPLACED",
			"outputs": [
				"3bcf09a5-0680-4826-8fef-76e79f31fad4",
				"0c0cdd6d-3686-4198-88c7-d0753dda6945"
			],
			"autoHeight": true,
			"components": [],
			"contentRef": "element_11_content",
			"cover": {
				"type": "image",
				"file": "/crusader.jpeg"
			}
		},
		"fa351fcb-f7cd-4b65-ae0a-4640e8a5275f": {
			"theme": "green",
			"title": "Correct answer",
			"content": "TO_BE_REPLACED",
			"outputs": [
				"ce4869fb-b31e-47a5-a0ca-47e46839a6f8"
			],
			"autoHeight": true,
			"components": [],
			"contentRef": "element_12_content",
			"cover": {
				"type": "image",
				"file": "/the door.jpeg"
			}
		}
	},
	"jumpers": {
		"17251b2a-0930-4a93-8f14-e89eb3238146": {
			"elementId": "59370898-e089-46cd-b7cf-072b79a76453"
		},
		"3517a1bc-3b73-4850-b03d-09daf9b7bc89": {
			"elementId": "81a30674-aa09-4eb8-8484-59298d37f984"
		},
		"60b22737-d18c-44ea-b24c-8d771dfd9f9d": {
			"elementId": "d4823bca-643b-4d3a-a908-0056762a0476"
		}
	},
	"connections": {
		"0c0cdd6d-3686-4198-88c7-d0753dda6945": {
			"type": "Bezier",
			"label": "\"I’ll come back later\"",
			"theme": "default",
			"sourceid": "d4823bca-643b-4d3a-a908-0056762a0476",
			"targetid": "3517a1bc-3b73-4850-b03d-09daf9b7bc89",
			"sourceType": "elements",
			"targetType": "jumpers"
		},
		"10b4a35e-8329-4a0d-86d2-3d0144254aa1": {
			"type": "Straight",
			"label": "\"Uh... Crusader?\"",
			"theme": "red",
			"sourceid": "2a94652d-594c-4e8d-9a05-993e9155547b",
			"targetid": "4c7d10f2-e68b-498f-9ced-ffd74977a8f5",
			"sourceType": "conditions",
			"targetType": "elements"
		},
		"1a09fe9a-062e-4dbb-8615-bf0aa455f783": {
			"type": "Straight",
			"label": "[i][ You remember the name on the painting ]\\n[/i]\"Conrad IV, Count of Exeter?\"",
			"theme": "green",
			"sourceid": "5caff231-4102-41f0-8eb6-755d48660fb7",
			"targetid": "fa351fcb-f7cd-4b65-ae0a-4640e8a5275f",
			"sourceType": "conditions",
			"targetType": "elements"
		},
		"337c4347-5b21-449c-b556-134e0acec12d": {
			"type": "Straight",
			"label": "Exit Castle Monsignor",
			"theme": "default",
			"sourceid": "59370898-e089-46cd-b7cf-072b79a76453",
			"targetid": "38375689-9e4d-430e-954c-65205eb622fe",
			"sourceType": "elements",
			"targetType": "elements"
		},
		"3461cb48-901b-49d5-99ba-dbfb69e6efe1": {
			"type": "Straight",
			"label": "Talk to the man",
			"theme": "default",
			"sourceid": "188e6385-85e2-496b-82ac-a81696e7bcab",
			"targetid": "60b22737-d18c-44ea-b24c-8d771dfd9f9d",
			"sourceType": "elements",
			"targetType": "jumpers"
		},
		"3bcf09a5-0680-4826-8fef-76e79f31fad4": {
			"type": "Straight",
			"label": "\"Can I pass?\"",
			"theme": "default",
			"sourceid": "d4823bca-643b-4d3a-a908-0056762a0476",
			"targetid": "d393f371-5aad-4dd9-a3ee-300bedaf993d",
			"sourceType": "elements",
			"targetType": "elements"
		},
		"64b12ec3-e4d4-475b-ab3b-d1187b043a04": {
			"type": "Straight",
			"label": null,
			"theme": "default",
			"sourceid": "d393f371-5aad-4dd9-a3ee-300bedaf993d",
			"targetid": "e4ef520d-1a30-4fd8-97f7-ed7f3ccb1426",
			"sourceType": "elements",
			"targetType": "branches"
		},
		"6738d031-bbd5-4d98-993f-a4926fc7fa33": {
			"type": "Bezier",
			"label": null,
			"theme": "red",
			"sourceid": "4c7d10f2-e68b-498f-9ced-ffd74977a8f5",
			"targetid": "4d61d93e-e541-4dff-b10d-42c4fd81ad0b",
			"sourceType": "elements",
			"targetType": "elements"
		},
		"678e093e-537c-4161-b011-8d6763c4be14": {
			"type": "Straight",
			"label": "Follow the path north",
			"theme": "default",
			"sourceid": "59370898-e089-46cd-b7cf-072b79a76453",
			"targetid": "81a30674-aa09-4eb8-8484-59298d37f984",
			"sourceType": "elements",
			"targetType": "elements"
		},
		"6a929b6d-c6d2-46c4-ab67-b0503e7213af": {
			"type": "Straight",
			"label": "Back to Castle Courtyard",
			"theme": "default",
			"sourceid": "81a30674-aa09-4eb8-8484-59298d37f984",
			"targetid": "59370898-e089-46cd-b7cf-072b79a76453",
			"sourceType": "elements",
			"targetType": "elements"
		},
		"6f0952d7-202a-4978-8d63-3fee4c5dde35": {
			"type": "Bezier",
			"label": "Back to Castle Courtyard ",
			"theme": "default",
			"sourceid": "5e0d8b0c-ae7c-42a6-83d1-fc2c13b8e6b4",
			"targetid": "17251b2a-0930-4a93-8f14-e89eb3238146",
			"sourceType": "elements",
			"targetType": "jumpers"
		},
		"80407dcb-6a30-4720-8d87-0d53419c4d30": {
			"type": "Bezier",
			"label": null,
			"theme": "red",
			"sourceid": "601034d9-b442-449a-a927-cd112fca8f7e",
			"targetid": "4d61d93e-e541-4dff-b10d-42c4fd81ad0b",
			"sourceType": "elements",
			"targetType": "elements"
		},
		"8ec9d83d-02cd-41fe-a16b-a7773e1ec678": {
			"type": "Straight",
			"label": "Step back from the painting",
			"theme": "default",
			"sourceid": "b2eb06f5-0a49-488b-8a60-7c469fd3af8a",
			"targetid": "5e0d8b0c-ae7c-42a6-83d1-fc2c13b8e6b4",
			"sourceType": "elements",
			"targetType": "elements"
		},
		"9b8efafe-a73f-4c19-8b90-0cf18b7e4907": {
			"type": "Straight",
			"label": "Walk further down the path",
			"theme": "default",
			"sourceid": "81a30674-aa09-4eb8-8484-59298d37f984",
			"targetid": "188e6385-85e2-496b-82ac-a81696e7bcab",
			"sourceType": "elements",
			"targetType": "elements"
		},
		"a21bbb0e-2496-4cd9-81a5-08773b3a4044": {
			"type": "Bezier",
			"label": "Try to run past him",
			"theme": "red",
			"sourceid": "d393f371-5aad-4dd9-a3ee-300bedaf993d",
			"targetid": "601034d9-b442-449a-a927-cd112fca8f7e",
			"sourceType": "elements",
			"targetType": "elements"
		},
		"b7f7dfc4-46f9-40ee-a8e9-e8b584cecadf": {
			"type": "Straight",
			"label": "Enter the castle",
			"theme": "default",
			"sourceid": "38375689-9e4d-430e-954c-65205eb622fe",
			"targetid": "59370898-e089-46cd-b7cf-072b79a76453",
			"sourceType": "elements",
			"targetType": "elements"
		},
		"bd5fbb0a-481c-4b6a-8c77-21b153c4bb95": {
			"type": "Straight",
			"label": "Back to the stone passage",
			"theme": "default",
			"sourceid": "188e6385-85e2-496b-82ac-a81696e7bcab",
			"targetid": "81a30674-aa09-4eb8-8484-59298d37f984",
			"sourceType": "elements",
			"targetType": "elements"
		},
		"c903fd63-a07c-4d5f-829e-461eee7ce2b6": {
			"type": "Straight",
			"label": "Examine the painting",
			"theme": "default",
			"sourceid": "5e0d8b0c-ae7c-42a6-83d1-fc2c13b8e6b4",
			"targetid": "b2eb06f5-0a49-488b-8a60-7c469fd3af8a",
			"sourceType": "elements",
			"targetType": "elements"
		},
		"ce4869fb-b31e-47a5-a0ca-47e46839a6f8": {
			"type": "Straight",
			"label": "Open the door",
			"theme": "green",
			"sourceid": "fa351fcb-f7cd-4b65-ae0a-4640e8a5275f",
			"targetid": "b6b7cebe-0c30-4da2-9839-2639f458f759",
			"sourceType": "elements",
			"targetType": "elements"
		},
		"dd563c04-f2d3-4c7f-a66e-3bb649c34831": {
			"type": "Straight",
			"label": "\"I’ll come back later\"",
			"theme": "default",
			"sourceid": "d393f371-5aad-4dd9-a3ee-300bedaf993d",
			"targetid": "3517a1bc-3b73-4850-b03d-09daf9b7bc89",
			"sourceType": "elements",
			"targetType": "jumpers"
		},
		"e8fbe6f8-7ce4-404d-b693-d108d7125a75": {
			"type": "Straight",
			"label": "Enter the house",
			"theme": "default",
			"sourceid": "59370898-e089-46cd-b7cf-072b79a76453",
			"targetid": "5e0d8b0c-ae7c-42a6-83d1-fc2c13b8e6b4",
			"sourceType": "elements",
			"targetType": "elements"
		}
	},
	"branches": {
		"e4ef520d-1a30-4fd8-97f7-ed7f3ccb1426": {
			"theme": "default",
			"conditions": {
				"ifCondition": "5caff231-4102-41f0-8eb6-755d48660fb7",
				"elseCondition": "2a94652d-594c-4e8d-9a05-993e9155547b"
			}
		}
	},
	"components": {
		"036db898-0cb9-4a29-b19d-3e8f3288d9a2": {
			"name": "Characters",
			"children": [
				"647349ca-9ebb-4d5f-b81e-500761ec1c5a"
			]
		},
		"11e527ff-9579-412c-ae5c-6ff548ebcfc0": {
			"name": "Castle Courtyard",
			"attributes": [
				"b7aa7bdf-26eb-41b0-bcc3-54fe13a9a31c"
			],
			"cover": {
				"file": "delapouite/castle",
				"type": "icon"
			}
		},
		"647349ca-9ebb-4d5f-b81e-500761ec1c5a": {
			"name": "Crusader",
			"attributes": [
				"d46b5c77-6441-4d30-8d74-b5a191b5ffc6",
				"3e148d58-6b88-4976-94a0-188df6ce55da",
				"c9607db4-f1d3-468d-8dfe-0f9b493600d8"
			],
			"cover": {
				"type": "image",
				"file": "/crusader.jpeg"
			}
		},
		"65005bda-15d0-41c5-867f-68c118829f08": {
			"name": "Root",
			"root": true,
			"children": [
				"036db898-0cb9-4a29-b19d-3e8f3288d9a2",
				"e175e706-97ec-42a8-8791-7855b29396e7"
			]
		},
		"894c2a71-952e-45c9-b55a-340794307a1d": {
			"name": "Castle Monsignor",
			"attributes": [
				"a1f9c1b1-0514-466d-a71e-756916ec2e04",
				"dbedf071-8334-4ae1-8646-b23a93299fce"
			],
			"cover": {
				"type": "image",
				"file": "/monsignor.jpeg"
			}
		},
		"e175e706-97ec-42a8-8791-7855b29396e7": {
			"name": "Locations",
			"children": [
				"11e527ff-9579-412c-ae5c-6ff548ebcfc0",
				"894c2a71-952e-45c9-b55a-340794307a1d"
			]
		}
	},
	"attributes": {
		"3e148d58-6b88-4976-94a0-188df6ce55da": {
			"name": "Real name",
			"value": {
				"data": "Conrad IV, Count of Exeter",
				"type": "string"
			}
		},
		"a1f9c1b1-0514-466d-a71e-756916ec2e04": {
			"name": "Info",
			"value": {
				"data": "Caste Monsignor is where the game plot and narrative take place. It was built in 1562 near Devonshire, England. The Castle is home to Lord Monsignor, the player's uncle, who has been living there for the past 20 years. The castle has recently become a time traveling portal, allowing people from the 16th century to travel to the present.",
				"type": "string"
			}
		},
		"b7aa7bdf-26eb-41b0-bcc3-54fe13a9a31c": {
			"name": "Description",
			"value": {
				"data": "The courtyard of Castle Monsignor ",
				"type": "string"
			}
		},
		"c9607db4-f1d3-468d-8dfe-0f9b493600d8": {
			"name": "Defeated by",
			"value": {
				"data": "Telling him his real name, which can be found on a painting inside the small house",
				"type": "string"
			}
		},
		"d46b5c77-6441-4d30-8d74-b5a191b5ffc6": {
			"name": "Description",
			"value": {
				"data": "The Crusader exists as an early obstacle to the player, introducing an atmosphere of danger and anachronism throughout the game",
				"type": "string"
			}
		},
		"dbedf071-8334-4ae1-8646-b23a93299fce": {
			"name": "Characters living here",
			"value": {
				"data": [
					"647349ca-9ebb-4d5f-b81e-500761ec1c5a"
				],
				"type": "component-list"
			}
		}
	},
	"variables": {
		"747d0495-341f-42b2-8788-b47dace22bf8": {
			"root": true,
			"children": [
				"990d6321-43cb-430b-84bd-6b48c574c489"
			]
		},
		"990d6321-43cb-430b-84bd-6b48c574c489": {
			"name": "paintingExamined",
			"type": "boolean",
			"value": false
		}
	},
	"conditions": {
		"2a94652d-594c-4e8d-9a05-993e9155547b": {
			"output": "10b4a35e-8329-4a0d-86d2-3d0144254aa1",
			"script": null
		},
		"5caff231-4102-41f0-8eb6-755d48660fb7": {
			"output": "1a09fe9a-062e-4dbb-8615-bf0aa455f783",
			"script": "(state.get_var(\"paintingExamined\") == true)"
		}
	},
	"name": "The Castle",
	"cover": {
		"file": "cover.jpg",
		"type": "template-image"
	}
}

func _init(utils: Utils):
	self.name = data.name
	self.starting_element = data.startingElement
	self.utils = utils

	for element_id in self.data.elements:
		if self.data.elements[element_id].content and self.data.elements[element_id].contentRef:
			self.data.elements[element_id].content = funcref(self, self.data.elements[element_id].contentRef)

func element_0_content(state):
	var content_result: String = ""
	content_result += "You reach a wooden door. In front of you stands a strange man dressed as a medieval crusader.\n "
	return content_result.trim_suffix(" ")

func element_1_content(state):
	var content_result: String = ""
	content_result += "You just arrived at Castle Monsignor, your uncle's home for the past 20 years. The tone of his letter was desperate, urgently asking for your help. Worried, you quickly packed your suitcase and took the first plane.\nFaint bird sounds break the eerie silence and a pale mist engulfs the castle hill.\n[b]To the north you can see the castle entrance.[/b]\n "
	return content_result.trim_suffix(" ")

func element_2_content(state):
	var content_result: String = ""
	content_result += "[b]\"Wrong!\", [/b]he shouts. Before you can utter another word he strikes you with the back of his sword hilt. \nEverything turns to black...\n "
	return content_result.trim_suffix(" ")

func element_3_content(state):
	var content_result: String = ""
	content_result += "Alas, you did not manage to find what lies behind the ominous door...\n "
	return content_result.trim_suffix(" ")

func element_4_content(state):
	var content_result: String = ""
	content_result += "You stand inside a small courtyard. Everything is unusually silent here.\n[b]The path leads further to the north. An abandoned house lies to your left.[/b]\n "
	return content_result.trim_suffix(" ")

func element_5_content(state):
	var content_result: String = ""
	content_result += "The interior looks more like an artist's studio than a house. A few abstract paintings lie scattered around.\nOne of the paintings depicts a strange figure: a knight dressed in white, with a red cross on his chest.\n "
	return content_result.trim_suffix(" ")

func element_6_content(state):
	var content_result: String = ""
	content_result += "As you try to run past him, he swiftly grabs you by the neck. You fight for freedom but his grip is inhumanly strong. \nYou drift into unconsciousness...\n "
	return content_result.trim_suffix(" ")

func element_7_content(state):
	var content_result: String = ""
	content_result += "A stone passage. The sound of dripping water is echoing in the distance. You feel uneasy.\n "
	return content_result.trim_suffix(" ")

func element_8_content(state):
	var content_result: String = ""
	content_result += "The knight is guarding a mysterious gate. You see a name written above his head:\n[quote]\"Conrad IV, Count of Exeter\"\n[/quote] "
	if not state.get_var("paintingExamined"):
		content_result += "\nYou make a mental note of the knight's name.\n "
	state.set_var("paintingExamined", true)
	return content_result.trim_suffix(" ")

func element_9_content(state):
	var content_result: String = ""
	content_result += "Congrats, you finished the example project! 🎉Now try and make your own interactive experience. If you need help check the [url=https://arcweave.com/docs/1.0]docs[/url], our [url=https://youtu.be/Dfm6dKQH3I0]Youtube tutorials[/url] or ping us on [url=https://discord.gg/atVXxAK]Discord[/url]!\nHappy creations! 😊\n "
	return content_result.trim_suffix(" ")

func element_10_content(state):
	var content_result: String = ""
	content_result += "[b]\"Only he who knows my name shall pass.\"[/b]\n "
	return content_result.trim_suffix(" ")

func element_11_content(state):
	var content_result: String = ""
	content_result += "The man looks at you suspiciously and tightens his sword grip. He speaks in a deep voice:\n[b]\"What do you want stranger?\"[/b]\n "
	return content_result.trim_suffix(" ")

func element_12_content(state):
	var content_result: String = ""
	content_result += "In the blink of an eye the Crusader disappears... Almost as if he was never there. Curious.\n "
	return content_result.trim_suffix(" ")

func get_data():
	return self.data

func _to_string():
	return JSON.print({
		"name": self.name,
		"components": self.components,
		"variables": self.variables
	}, "\t")
