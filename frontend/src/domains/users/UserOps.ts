import { UserRoles, type IUser } from './User';

/** Questions about a member that more than one component needs to ask. */
const UserOps = {
  isModerator(user: IUser | null): boolean {
    return user?.role === UserRoles.Moderator;
  },

  isOwner(user: IUser | null, authorId: string): boolean {
    return user?.id === authorId;
  },
} as const;

export default UserOps;
