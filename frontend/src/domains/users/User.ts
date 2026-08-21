/***** Constants *****/

/** What a member is allowed to do beyond posting. */
export const UserRoles = {
  Member: 'Member',
  Moderator: 'Moderator',
} as const;

export type UserRole = (typeof UserRoles)[keyof typeof UserRoles];

/***** Types *****/

/** A member, as the API describes them. */
export interface IUser {
  id: string;
  username: string;
  email: string;
  role: UserRole;
}

/** Said in reply to anything that must not reveal whether an account exists. */
export interface IAcknowledgement {
  message: string;
}
