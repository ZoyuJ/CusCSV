
export class Table {
  public readonly Header: Header | null = null;
  public readonly Rows: Row[] = [];

  public readonly HasHeader: boolean = true;

  public NextChar(Char1: string) {
    if (this.HasHeader && this.Rows?.length === 0) {
      const Next = this.Header.NextChar(Char1);
      if (Next !== null) this.Rows.push(Next);
    }
    else {
      const Next = this.Rows[this.Rows.length - 1].NextChar(Char1);
      if (Next !== null) this.Rows.push(Next);
    }
  }

  public ToString() {
    return `${this.Rows.join("\n\r")}`;
  }
  public ToStringWithHeader() {
    return `${this.Header.ToString()}\n\r${this.ToString()}`;
  }

  *[Symbol.iterator]() {
    for (let Row of this.Rows) {
      yield Row;
    }
  }

}
export class Row {
  public readonly Fields: Field[] | null = null;

  constructor() { }

  public NextChar(Char1: string): Row {
    const Next = this.Fields[this.Fields.length - 1].NextChar(Char1);
    if (Next.Eof) {
      return new Row();
    }
    else if (Next.Field != null) {
      this.Fields.push(Next.Field);
    }
    return null;
  }

  public ToString(): string {
    return `${this.Fields.join(",")}`;
  }

  *[Symbol.iterator]() {
    for (let Field of this.Fields) {
      yield Field;
    }
  }
}
export class Field {
  private _Chars: string[];
  private RawText: string | null = null;
  private Text: string | null = null;
  private Enclosed: boolean;

  public IsEnclosed() { return this.Enclosed; }
  public ToString() { return this.RawText; }

  public NextChar(Char1: string): Eof {
    const Eof = { Eof: false, Field: null };
    if (Char1 === "\"") this.Enclosed = !this.Enclosed;
    if (this.Enclosed) {
      if (Char1 === ",") {
        Eof.Field = new Field();
        return Eof;
      }
      else if (Char1 === "\r" && this._Chars.length > 0 && this._Chars[this._Chars.length - 1] === "\n") {
        Eof.Eof = true;
        this._Chars.slice(this._Chars.length - 1, 1);
        return Eof;
      }
    }
    if (!(Char1 === "\"" && (this._Chars.length > 0 && this._Chars[this._Chars.length - 1] === "\""))) this._Chars.push(Char1);
    return null;
  }

  private DealObject(Value: any): string {
    if (Value instanceof Date) {
      return (Value as Date).toISOString();
    }
    return Value.ToString();
  }
  public SetValue(Value: any) {
    this.SetText(this.DealObject(Value));
  }
  public SetText(Text: string) {
    this.Text = Text;
    this._Chars = [];
    for (let i = this.Text.length - 1; i >= 0; i--) {
      this._Chars.push(this.Text[i]);
      if (this.Text[i] == "\"") {
        this._Chars.push("\"");
      }
      else if (!this.Enclosed && (this.RawText[i] === "," || (this.RawText[i] === "\r" && i > 0 && this.RawText[i - 1] === "\n"))) this.Enclosed = true;
    }
    if (this.Enclosed) {
      this._Chars.push("\"");
      this._Chars.unshift("\"");
    }
    this.RawText = this._Chars.join("");
  }

  constructor() { }
  public static FromCSVText(Text: string): Field {
    const F = new Field();
    F.SetText(Text);
    return F;
  }
  public static FormObject(Value: any): Field {
    const F = new Field();
    F.SetValue(Value);
    return F;
  }


}
export class Header extends Row {

}

interface Eof {
  Eof: boolean;
  Field?: Field | null;
}