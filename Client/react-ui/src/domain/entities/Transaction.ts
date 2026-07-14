import { Money } from '../value-objects/Money';

export interface TransactionProps {
    id: string;
    maskedCard: string;
    money: Money;
    ruleName: string; 
    suspicionReason: string;
    riskScore: number; 
    location: string;
    date: string;
    fraudReason?: string;
}

export class Transaction {
    private readonly props: TransactionProps;

    constructor(props: TransactionProps) {
        this.props = props;
    }

    get id(): string { return this.props.id; }
    get maskedCard(): string { return this.props.maskedCard; }
    get money(): Money { return this.props.money; }
    
    get ruleName(): string { return this.props.ruleName; }
    get suspicionReason(): string { return this.props.suspicionReason; }
    get riskScore(): number { return this.props.riskScore; }
    
    get location(): string { return this.props.location; }
    get date(): string { return this.props.date; }
    
    // YENİ EKLENDİ: Sidebar'ın okuyabilmesi için Getter açtık
    get fraudReason(): string | undefined { return this.props.fraudReason; } 
}