using Iriha.Compiler;

const string Input = """
func main(argc: int, argv: &&byte): int {
	
}

func M<T>(val: T): T { return val; }

func sample(value: &int, list: Core::List<int>): int {
	let another_ref: &int = value;
	let deref: int = *value;
	let x: int = deref + 3;

	let y: int = list.At(3);

	let eq1: bool = x == deref;
	let eq2: bool = x < deref;
	mut eq3: bool = eq1 && eq2;

	let bitwised: int = x & deref;

	_ = sample(x, 3);

	sample(x, 4);

	mut ret: int = { 
		let temp: int = 3;
		yield temp;
	};

	return 3;
}

struct Test {
	X: int;
}
""";

new Compilation("TestCompilation").Compile(Input);
