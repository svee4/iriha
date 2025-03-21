A programming language that is under construction and looks something like this
```
func main(argc: int, argv: &&char): int {
	return argc >> $(something($1), argc) >> $1 / $2;
}

func something(v: int): int {
	return v * 2;
}

struct Test {
	X: int;
}
```
