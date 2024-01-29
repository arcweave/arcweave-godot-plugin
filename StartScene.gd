extends Node2D

func _on_godot_button_pressed():
	get_tree().change_scene_to_file("res://GodotScene.tscn")


func _on_c_sharp_button_pressed():
	get_tree().change_scene_to_file("res://CSharpScene.tscn")
