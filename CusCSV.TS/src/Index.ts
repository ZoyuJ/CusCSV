import { Func1 } from "katkits/lib/Event";

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
  public Table: Table;
  constructor(Table: Table) {
    this.Table = Table;
  }

  public NextChar(Char1: string): Row {
    const Next = this.Fields[this.Fields.length - 1].NextChar(Char1);
    if (Next.Eof) {
      let i = 0;
      this.Fields.forEach(E => {
        E.ColIndex = i++;
      });
      return new Row(this.Table);
    }
    else if (Next.Field != null) {
      this.Fields.push(Next.Field);
    }
    return null;
  }

  public ToString(): string {
    return `${this.Fields.join(",")}`;
  }

  public ToObject(): any[] {
    const Arr = [];
    this.Fields.forEach(E => Arr.push(E.ToValue()));
    return Arr;
  }



  *[Symbol.iterator]() {
    for (let Field of this.Fields) {
      yield Field;
    }
  }
}
export class Field {
  protected _Chars: string[];
  public RawText: string | null = null;
  public Text: string | null = null;
  protected Enclosed: boolean;
  public Table: Table;
  public ColIndex: number;
  public Header(): Field {
    if (this.Table.HasHeader && this.Table.Rows.length > 0) {
      return this.Table.Header.PeekHeader(this.ColIndex);
    }
    return null;
  }

  public IsEnclosed() { return this.Enclosed; }
  public ToString() { return this.RawText; }

  public NextChar(Char1: string): Eof {
    const Eof = { Eof: false, Field: null };
    if (Char1 === "\"") this.Enclosed = !this.Enclosed;
    if (this.Enclosed) {
      if (Char1 === ",") {
        Eof.Field = new Field(this.Table);
        return Eof;
      }
      else if (Char1 === "\r" && this._Chars.length > 0 && this._Chars[this._Chars.length - 1] === "\n") {
        Eof.Eof = true;
        this._Chars.slice(this._Chars.length - 1, 1);
        return Eof;
      }
    }
    if (!(Char1 === "\"" && (this._Chars.length > 0 && this._Chars[this._Chars.length - 1] === "\"")))
      this._Chars.push(Char1);
    return null;
  }


  public SetValue(Value: any) {
    this.SetText(this.Table.Header.PeekConverter(this.ColIndex).To(Value));
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

  public ToValue(): any {
    return this.Table.Header.PeekConverter(this.ColIndex).From(this.Text);
  }

  constructor(Table: Table) {
    this.Table = Table;
  }
  public static FromCSVText(Row: Row, Text: string): Field {
    const F = new Field(Row.Table);
    F.SetText(Text);
    return F;
  }
  public static FormObject(Row: Row, Value: any): Field {
    const F = new Field(Row.Table);
    F.SetValue(Value);
    return F;
  }


}

export interface Converter {
  To: Func1<any, string>;
  From: Func1<any, string>;
}
export class Header extends Row {
  protected Converters: Converter[];

  public static FullbackConverter(): Converter {
    return {
      To: Header.FullbackToTextConverter,
      From: Header.FullbackFromTextConverter,
    }
  }
  private static FullbackToTextConverter(Value: any): string {
    if (Value instanceof Date) {
      return (Value as Date).toISOString();
    }
    return Value.ToString();
  }
  private static FullbackFromTextConverter(Text: string): string { return Text; }

  public PeekHeader(ColIndex: number) {
    if (this.Fields.length > 0 && ColIndex < this.Fields.length) {
      return this.Fields[ColIndex];
    }
    return null;
  }
  public PeekConverter(ColIndex: number): Converter {
    return this.Converters[ColIndex];
  }


  public NextChar(Char1: string) {
    const Next = this.Fields[this.Fields.length - 1].NextChar(Char1);
    if (Next.Eof) {
      this.Converters = []
      let i = 0;
      this.Fields.forEach(E => {
        E.ColIndex = i++;
        this.Converters.push(Header.FullbackConverter());
      });
      return new Row(this.Table);
    }
    else if (Next.Field != null) {
      if (Next.Field.Text === '' && this.Fields.length > 0) {
        Next.Field.Text = this.Fields[this.Fields.length].Text;
      }
      this.Fields.push(Next.Field);
    }
    return null;
  }

  constructor(Table: Table, FirstRow: Row) {
    super(Table);
  }


}

export default class CSV {
  public static FromCSV(CSVText: string): Table {
    const Tb = new Table();
    for (const C of CSVText) {
      Tb.NextChar(C);
    }
    return Tb;
  }

  public static ToCSV(Table: Table): string {
    return Table.ToStringWithHeader();
  }

  public static readonly MIMEType: string = "text/csv";
}

interface Eof {
  Eof: boolean;
  Field?: Field | null;
  Row?: Row | null;
  LastIndex?: number;
}