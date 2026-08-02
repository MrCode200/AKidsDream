shader_type canvas_item;

uniform vec4 color:source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform int type: hint_enum("Disable", "Round", "Square") = 2;
uniform float thickness:hint_range(0, 10) = 1.0;
// uniform vec4 flicker_color: source_color = vec4(0.0, 0.0, 0.0, 0.0); // Add maybe later
uniform float flickering_speed: hint_range(0.0, 25.0, .01);
uniform float minimal_flickering_alpha: hint_range(0.0, 1);

const vec2[8] DIRECTIONS = {
	vec2(1.0, 0.0),
	vec2(0.0, 1.0),
	vec2(-1.0, 0.0),
	vec2(0.0, -1.0),
	vec2(1.0, 1.0),
	vec2(-1.0, 1.0),
	vec2(-1.0, -1.0),
	vec2(1.0, -1.0)
};

float gtz(float input) { return max(0, sign(input)); }
// returns 1 if input > 0, else 0

float check(sampler2D tex, vec2 from, vec2 size) {
	float result = 0.0;
	for (int i = 0; i < 4 * type; i ++)
	{
		result += texture(tex, from + DIRECTIONS[i] * size * thickness).a;
	}
	return gtz(result);
}

void fragment() {
    float outline = check(TEXTURE, UV, TEXTURE_PIXEL_SIZE) * (1.0 - gtz(COLOR.a));
	
	float flicker = mix(
		minimal_flickering_alpha,
		1.0,
		0.5 + 0.5 * sin(TIME * flickering_speed)
	);
	COLOR = mix( COLOR, color, outline * flicker);
}
