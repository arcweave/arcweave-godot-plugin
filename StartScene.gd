extends Control

func _on_godot_button_pressed():
	get_tree().change_scene_to_file("res://scenes/gd_script_scene.tscn")


func _on_c_sharp_button_pressed():
	get_tree().change_scene_to_file("res://scenes/c_sharp_scene.tscn")
