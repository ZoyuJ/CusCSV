
class Table {
  public readonly Header: Header | null = null;
  public readonly Rows: Row[] | null = null;

  *[Symbol.iterator]() {
    for (let Row of this.Rows) {
      yield Row;
    }
  }

}
class Row {
  public readonly Fields: Field[] | null = null;

  *[Symbol.iterator]() {
    for (let Field of this.Fields) {
      yield Field;
    }
  }
}
class Field {
  public Raw: string | null = null;

  public SetTextValue(Text: string) {
    this.Raw = Text;
    for (let i = this.Raw.length - 1; i >= 0; i--) {

    }
  }

}
class Header extends Row {

}

interface IIndexer<TValue> {
  [Index: number]: TValue;
}
interface IMapper<TValue> {
  [Key: string]: TValue
}