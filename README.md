A programming language that is under construction and looks something like this
```
func main(argc: int, argv: &&char): int {
	
}

func take_ref(value: &int): void {
	var another_ref: &int = value;
	var deref: int = *value;
	var x: int = deref + 3;

	# random_method(x, 3);
}

struct Test {
	X: int;
}
```
