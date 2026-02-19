shader_type canvas_item;

vec4 screen_blend(vec4 base, vec4 blend){
	return 1.0 - (1.0 - base) * (1.0 - blend);
}

void fragment(){
	vec4 screen = texture(SCREEN_TEXTURE, SCREEN_UV);
	vec4 sprite = texture(TEXTURE, UV);
	COLOR = screen_blend(screen, sprite);
	COLOR.a = sprite.a;
}
