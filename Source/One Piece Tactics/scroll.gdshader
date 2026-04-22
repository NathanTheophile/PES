shader_type canvas_item;

uniform vec2 d = vec2(1,0);
uniform float s = 0.1;

void fragment(){
	COLOR = texture(TEXTURE, UV + (d*TIME*s));
}